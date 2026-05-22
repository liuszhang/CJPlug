using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

namespace CSharpPlug.Services
{
    public class CodeRunner
    {
        private static readonly string[] _frameworkAssemblies = new[]
        {
            "System.Runtime.dll",
            "System.Linq.dll",
            "System.Collections.dll",
            "System.Console.dll",
            "System.IO.dll",
            "System.Text.Json.dll",
            "System.Text.RegularExpressions.dll",
            "System.ComponentModel.Primitives.dll",
            "System.ComponentModel.TypeConverter.dll",
            "System.Private.Uri.dll",
            "netstandard.dll",
        };

        private static List<MetadataReference>? _cachedFrameworkRefs;

        private static List<MetadataReference> GetFrameworkReferences()
        {
            if (_cachedFrameworkRefs != null)
                return _cachedFrameworkRefs;

            var refs = new List<MetadataReference>();
            var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();

            foreach (var asm in _frameworkAssemblies)
            {
                var path = Path.Combine(runtimeDir, asm);
                if (File.Exists(path))
                {
                    refs.Add(MetadataReference.CreateFromFile(path));
                }
            }

            // Ensure core types (object, string, etc.) are available
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            _cachedFrameworkRefs = refs;
            return refs;
        }

        public static async Task<string> RunCSharpCodeAsync(string code, string[] references)
        {
            var allRefs = new List<MetadataReference>(GetFrameworkReferences());
            foreach (var r in references)
            {
                if (File.Exists(r))
                    allRefs.Add(MetadataReference.CreateFromFile(r));
            }

            var options = ScriptOptions.Default
                .WithReferences(allRefs)
                .WithImports("System", "System.Collections.Generic", "System.Linq", "System.IO", "System.Text");

            code = WrapCodeIfHasMain(code);

            try
            {
                var result = await CSharpScript.EvaluateAsync(code, options);
                return result?.ToString() ?? "执行成功，但无返回值";
            }
            catch (Exception ex)
            {
                return $"执行错误: {ex.Message}";
            }
        }

        public static async Task<string> RunCSharpCodeWithDllsAsync(string code, List<string> dllPaths)
        {
            var references = new List<MetadataReference>(GetFrameworkReferences());

            foreach (var dllPath in dllPaths)
            {
                if (!File.Exists(dllPath)) continue;
                try
                {
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
                    references.Add(MetadataReference.CreateFromFile(dllPath));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载 DLL 失败: {dllPath}, 错误: {ex.Message}");
                }
            }

            var options = ScriptOptions.Default
                .WithReferences(references)
                .WithImports("System", "System.Collections.Generic", "System.Linq", "System.IO", "System.Text");

            code = WrapCodeIfHasMain(code);

            try
            {
                var result = await CSharpScript.EvaluateAsync(code, options);
                return result?.ToString() ?? "执行成功，但无返回值";
            }
            catch (Exception ex)
            {
                return $"执行错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 如果代码包含 static void Main，提取 Main 方法体作为脚本执行
        /// </summary>
        private static string WrapCodeIfHasMain(string code)
        {
            // 简单检测：是否包含 "static void Main"
            var mainIdx = code.IndexOf("static void Main", StringComparison.Ordinal);
            if (mainIdx < 0)
                return code;

            // 找到 Main 方法体的起始花括号
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

            // 保留 Main 方法前的顶层 using 语句
            var preamble = string.Join("\n", code[..mainIdx]
                .Split('\n')
                .Where(line => line.TrimStart().StartsWith("using ")));

            // 提取 Main 方法体
            var body = code[(braceStart + 1)..braceEnd].Trim();

            if (string.IsNullOrWhiteSpace(preamble))
                return body;

            return preamble + "\n" + body;
        }

        // ═══════════════════════════════════════════════════════════
        //  桥接模式 — 通过 .NET Framework 4.8 子进程执行
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 查找桥接可执行文件 DotnetCoreBridgeToDotnetFramework.exe
        /// </summary>
        private static string? FindBridgeExe()
        {
            var searchDirs = new List<string>();

            // 1. 与当前程序集同目录
            var asmDir = Path.GetDirectoryName(typeof(CodeRunner).Assembly.Location);
            if (!string.IsNullOrEmpty(asmDir))
            {
                searchDirs.Add(asmDir);
                searchDirs.Add(Path.Combine(asmDir, "bridge"));
            }

            // 2. 从程序集目录向上找到解决方案根目录，再定位 bridge 输出
            if (!string.IsNullOrEmpty(asmDir))
            {
                var dir = asmDir;
                for (int i = 0; i < 10; i++)
                {
                    dir = Path.GetDirectoryName(dir);
                    if (dir == null) break;

                    var bridgePath = Path.Combine(dir,
                        "src", "PlugToolIntegrations", "DotnetCoreBridgeToDotnetFramework",
                        "bin", "Debug", "net48", "DotnetCoreBridgeToDotnetFramework.exe");
                    searchDirs.Add(bridgePath);

                    bridgePath = Path.Combine(dir,
                        "src", "PlugToolIntegrations", "DotnetCoreBridgeToDotnetFramework",
                        "bin", "Release", "net48", "DotnetCoreBridgeToDotnetFramework.exe");
                    searchDirs.Add(bridgePath);
                }
            }

            // 3. 环境变量
            var envPath = Environment.GetEnvironmentVariable("CSHARP_BRIDGE_PATH");
            if (!string.IsNullOrEmpty(envPath))
                searchDirs.Add(envPath);

            foreach (var candidate in searchDirs)
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                    return full;
            }

            return null;
        }

        /// <summary>
        /// 通过 .NET Framework 4.8 桥接进程执行代码（适用于依赖 Framework-only DLL 的场景）
        /// </summary>
        public static async Task<string> RunCSharpCodeViaBridgeAsync(string code, List<string> dllPaths)
        {
            var bridgeExe = FindBridgeExe();
            if (bridgeExe == null)
                return "执行错误: 找不到 .NET Framework 桥接程序 (DotnetCoreBridgeToDotnetFramework.exe)。" +
                       "请确保桥接项目已编译，或设置环境变量 CSHARP_BRIDGE_PATH。";

            // 将代码写入临时文件，避免命令行长度限制
            var codeFile = Path.GetTempFileName() + ".cs";
            await File.WriteAllTextAsync(codeFile, code);

            try
            {
                var dllsArg = string.Join(";", dllPaths.Where(File.Exists));
                var args = $"--codefile \"{codeFile}\" --dlls \"{dllsArg}\"";

                var psi = new ProcessStartInfo(bridgeExe, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return "执行错误: 无法启动 .NET Framework 桥接进程。";

                var stdout = await process.StandardOutput.ReadToEndAsync();
                var stderr = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                    return $"执行错误: {stderr.Trim()}";

                return stdout.Trim();
            }
            catch (Exception ex)
            {
                return $"执行错误（桥接进程异常）: {ex.Message}";
            }
            finally
            {
                try { File.Delete(codeFile); } catch { }
            }
        }
    }
}
