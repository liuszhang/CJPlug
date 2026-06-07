using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DotnetCoreBridgeToDotnetFramework
{
    /// <summary>
    /// .NET Framework 桥接进程 — 在 net48 环境中编译执行用户 C# 代码。
    /// 
    /// 用法: DotnetCoreBridgeToDotnetFramework.exe --codefile &lt;path&gt; --dlls &lt;dll1&gt;;&lt;dll2&gt;;...
    /// 
    /// 输入:  代码写入临时文件（避免命令行长度限制）
    /// 输出:  stdout → 执行结果
    ///        stderr → 错误信息
    /// 退出码: 0 = 成功, 1 = 错误
    /// </summary>
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // 强制 UTF-8 输出，避免中文乱码
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            try
            {
                // ── 解析命令行参数 ──
                string? codeFile = null;
                var dllPaths = new List<string>();

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--codefile" && i + 1 < args.Length)
                        codeFile = args[++i];
                    else if (args[i] == "--dlls" && i + 1 < args.Length)
                        dllPaths.AddRange(args[++i].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                }

                if (string.IsNullOrEmpty(codeFile) || !File.Exists(codeFile))
                {
                    Console.Error.WriteLine("ERROR: 未指定代码文件或文件不存在");
                    return 1;
                }

                // ── 读取代码 ──
                var code = File.ReadAllText(codeFile);

                // ── 构造编译引用 ──
                var references = new List<MetadataReference>
                {
                    // .NET Framework 核心程序集
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),       // mscorlib
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),    // System.Core
                    MetadataReference.CreateFromFile(typeof(System.Xml.XmlDocument).Assembly.Location), // System.Xml
                    MetadataReference.CreateFromFile(typeof(System.Data.DataSet).Assembly.Location),    // System.Data
                };

                // 用户上传的 DLL — 统一复制到同一临时目录，托管程序集加入编译引用
                var dllDir = Path.Combine(Path.GetTempPath(), "CSharpBridge_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dllDir);

                foreach (var dllPath in dllPaths)
                {
                    if (!File.Exists(dllPath)) continue;

                    // 1. 统一复制到临时目录（供运行时 P/Invoke 互发现）
                    try
                    {
                        var dest = Path.Combine(dllDir, Path.GetFileName(dllPath));
                        File.Copy(dllPath, dest, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"WARNING: 无法复制 DLL {Path.GetFileName(dllPath)}: {ex.Message}");
                        continue;
                    }

                    // 2. 托管程序集 → 加入编译引用
                    if (IsManagedAssembly(dllPath))
                    {
                        try
                        {
                            references.Add(MetadataReference.CreateFromFile(dllPath));
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"WARNING: 无法加载托管 DLL {Path.GetFileName(dllPath)}: {ex.Message}");
                        }
                    }
                }

                // 将统一 DLL 目录加入 Windows 搜索路径（AddDllDirectory 保留默认搜索顺序）
                AddDllDirectory(dllDir);

                // 从父进程传入的通用 DLL 搜索目录列表（分号分隔）
                var dllDirs = Environment.GetEnvironmentVariable("BRIDGE_DLL_DIRS");
                if (!string.IsNullOrEmpty(dllDirs))
                {
                    foreach (var dir in dllDirs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Directory.Exists(dir))
                            AddDllDirectory(dir);
                    }
                }

                // 退出时清理
                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    try { Directory.Delete(dllDir, recursive: true); } catch { }
                };

                // ── 脚本选项 ──
                var options = ScriptOptions.Default
                    .WithReferences(references)
                    .WithImports(
                        "System",
                        "System.Collections.Generic",
                        "System.Linq",
                        "System.IO",
                        "System.Text",
                        "System.Threading.Tasks");

                // ── 处理 Main 方法 ──
                code = WrapCodeIfHasMain(code);

                // ── 执行 ──
                var result = await CSharpScript.EvaluateAsync(code, options);
                Console.WriteLine(result?.ToString() ?? "执行成功（无返回值）");
                return 0;
            }
            catch (CompilationErrorException cex)
            {
                var errors = string.Join("\n", cex.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
                Console.Error.WriteLine($"编译错误:\n{errors}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"执行错误: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// 如果代码包含 static void Main，提取 Main 方法体作为脚本执行。
        /// 保留 using 语句。
        /// </summary>
        private static string WrapCodeIfHasMain(string code)
        {
            var mainIdx = code.IndexOf("static void Main", StringComparison.Ordinal);
            if (mainIdx < 0)
                return code;

            var braceStart = code.IndexOf('{', mainIdx);
            if (braceStart < 0)
                return code;

            // 找到匹配的结束花括号
            var depth = 0;
            var braceEnd = -1;
            for (int i = braceStart; i < code.Length; i++)
            {
                if (code[i] == '{') depth++;
                else if (code[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        braceEnd = i;
                        break;
                    }
                }
            }

            if (braceEnd < 0)
                return code;

            // 保留 Main 前的 using 语句
            var preamble = string.Join("\n", code.Substring(0, mainIdx)
                .Split('\n')
                .Where(line => line.TrimStart().StartsWith("using ")));

            // 提取 Main 方法体
            var body = code.Substring(braceStart + 1, braceEnd - braceStart - 1).Trim();

            if (string.IsNullOrWhiteSpace(preamble))
                return body;

            return preamble + "\n" + body;
        }

        /// <summary>
        /// 判断 DLL 是否为 .NET 托管程序集（通过读取 PE 头中的 CLI 头）
        /// </summary>
        private static bool IsManagedAssembly(string dllPath)
        {
            try
            {
                // 快速判断：尝试用 AssemblyName.GetAssemblyName 读取
                // 如果不是有效程序集，会抛出 BadImageFormatException
                AssemblyName.GetAssemblyName(dllPath);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将目录加入 Windows DLL 搜索路径（保留默认搜索顺序，不替换）
        /// 需要先调用 SetDefaultDllDirectories 激活 LOAD_LIBRARY_SEARCH_USER_DIRS
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr AddDllDirectory(string NewDirectory);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDefaultDllDirectories(uint DirectoryFlags);

        private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        static Program()
        {
            // 启用 AddDllDirectory 支持（保留系统默认搜索路径）
            SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
        }
    }
}
