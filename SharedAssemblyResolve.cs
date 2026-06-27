// SharedAssemblyResolve.cs
// 当多个服务输出到同一共享目录时，各自的 .deps.json 会互相覆盖，
// 导致未被最后构建的服务的 NuGet 依赖无法通过 deps.json 定位。
// 此文件注册两个全局处理器作为兜底：
// 1. AssemblyResolve 事件 — 从共享目录加载托管 DLL
// 2. DllImportResolver — 从 runtimes/<rid>/native/ 加载原生库（如 e_sqlite3）
//
// 使用 [ModuleInitializer] 确保在 Main 方法执行前就注册处理器，
// 避免与 Program.cs 的顶级语句冲突（CS8802）。

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class SharedAssemblyResolve
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        NativeLibrary.SetDllImportResolver(typeof(SharedAssemblyResolve).Assembly, OnDllImport);
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            var name = new AssemblyName(args.Name).Name;
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name + ".dll");
            if (File.Exists(path))
                return Assembly.LoadFrom(path);
        }
        catch { }
        return null;
    }

    /// <summary>
    /// 原生库解析兜底：当 deps.json 被其他服务覆盖导致 runtimes/<rid>/native/ 下的
    /// 原生库（如 e_sqlite3.dll）无法被标准探测找到时，手动定位并加载。
    /// </summary>
    private static IntPtr OnDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        try
        {
            string rid;
            string ext;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                rid = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "win-x64",
                    Architecture.X86 => "win-x86",
                    Architecture.Arm64 => "win-arm64",
                    Architecture.Arm => "win-arm",
                    _ => "win-x64"
                };
                ext = ".dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
                ext = ".dylib";
            }
            else
            {
                rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
                ext = ".so";
            }

            var nativePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "runtimes", rid, "native",
                libraryName + ext);

            if (File.Exists(nativePath))
                return NativeLibrary.Load(nativePath);
        }
        catch { }

        return IntPtr.Zero; // 回退到默认解析
    }
}
