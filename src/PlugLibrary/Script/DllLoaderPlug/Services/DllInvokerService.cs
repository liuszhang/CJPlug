using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DllLoaderPlug.Services
{
    public class DllInvokeResult
    {
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IDllInvokerService
    {
        Task<DllInvokeResult> InvokeAsync(MethodDescriptor method);
        Task<DllInvokeResult> InvokeNativeAsync(MethodDescriptor method, string pinvokeSource);
        Task<Assembly?> LoadAssemblyAsync(string path);
    }

    public class DllInvokerService : IDllInvokerService
    {
        private readonly Dictionary<string, AssemblyLoadContext> _contexts = new();
        private readonly object _emitLock = new();

        public async Task<Assembly?> LoadAssemblyAsync(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            // reuse context per path
            if (_contexts.TryGetValue(path, out var existing))
            {
                return existing.Assemblies.FirstOrDefault(a => string.Equals(a.Location, path, StringComparison.OrdinalIgnoreCase));
            }

            var alc = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(path) + "_alc", isCollectible: true);
            try
            {
                using var fs = File.OpenRead(path);
                var asm = alc.LoadFromStream(fs);
                _contexts[path] = alc;
                return asm;
            }
            catch
            {
                try
                {
                    alc.Unload();
                }
                catch { }
                return null;
            }
        }

        public async Task<DllInvokeResult> InvokeAsync(MethodDescriptor method)
        {
            var res = new DllInvokeResult();
            try
            {
                if (method == null)
                {
                    res.Success = false;
                    res.ErrorMessage = "method is null";
                    return res;
                }

                if (string.IsNullOrEmpty(method.DeclaringType))
                {
                    res.Success = false;
                    res.ErrorMessage = "no declaring type";
                    return res;
                }

                // If this is a native export (from non-managed DLL), we cannot invoke it via reflection here.
                if (string.Equals(method.ReturnType, "native export", StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(method.DeclaringType) && method.DeclaringType.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                {
                    res.Success = false;
                    res.ErrorMessage = "所选方法来自本地非托管 DLL（native export）。请在编辑器中使用 P/Invoke 模板并点击测试调用（将尝试按模板生成并调用）。";
                    return res;
                }

                Assembly? assembly = null;
                if (!string.IsNullOrEmpty(method.SourcePath))
                {
                    assembly = await LoadAssemblyAsync(method.SourcePath!);
                }

                Type? type = null;
                if (assembly != null)
                {
                    type = assembly.GetType(method.DeclaringType, throwOnError: false, ignoreCase: false);
                }

                if (type == null)
                {
                    // try in default AppDomain
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            type = asm.GetType(method.DeclaringType, throwOnError: false, ignoreCase: false);
                            if (type != null) break;
                        }
                        catch { }
                    }
                }

                if (type == null)
                {
                    res.Success = false;
                    res.ErrorMessage = $"type {method.DeclaringType} not found";
                    return res;
                }

                var mi = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                             .FirstOrDefault(m => m.Name == method.Name);
                if (mi == null)
                {
                    res.Success = false;
                    res.ErrorMessage = $"method {method.Name} not found on type {method.DeclaringType}";
                    return res;
                }

                var parameters = mi.GetParameters();
                var args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var pType = parameters[i].ParameterType;
                    args[i] = pType.IsValueType ? Activator.CreateInstance(pType) : null;
                }

                object? instance = null;
                if (!mi.IsStatic)
                {
                    try
                    {
                        instance = Activator.CreateInstance(type);
                    }
                    catch (Exception ex)
                    {
                        res.Success = false;
                        res.ErrorMessage = $"create instance failed: {ex.Message}";
                        return res;
                    }
                }

                try
                {
                    var result = mi.Invoke(instance, args);
                    res.Success = true;
                    res.Result = result;
                    return res;
                }
                catch (TargetInvocationException tex)
                {
                    res.Success = false;
                    res.ErrorMessage = tex.InnerException?.Message ?? tex.Message;
                    return res;
                }
                catch (Exception ex)
                {
                    res.Success = false;
                    res.ErrorMessage = ex.Message;
                    return res;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.ErrorMessage = ex.Message;
                return res;
            }
        }

        public async Task<DllInvokeResult> InvokeNativeAsync(MethodDescriptor method, string pinvokeSource)
        {
            try
            {
                if (method == null) return new DllInvokeResult { Success = false, ErrorMessage = "method is null" };
                if (string.IsNullOrEmpty(pinvokeSource)) return new DllInvokeResult { Success = false, ErrorMessage = "pinvoke source empty" };

                var dllMatch = Regex.Match(pinvokeSource, "DllImport\\(\\\"(?<dll>[^\\\"]+)\\\"");
                if (!dllMatch.Success) return new DllInvokeResult { Success = false, ErrorMessage = "无法解析 DllImport 中的 DLL 路径" };
                var dllName = dllMatch.Groups["dll"].Value;

                var sigMatch = Regex.Match(pinvokeSource, @"extern\s+(?<ret>\w+)\s+(?<name>\w+)\s*\(\s*\)");
                if (!sigMatch.Success) return new DllInvokeResult { Success = false, ErrorMessage = "无法解析函数签名，请使用简单的无参签名示例" };
                var retType = sigMatch.Groups["ret"].Value;
                var funcName = sigMatch.Groups["name"].Value;

                lock (_emitLock)
                {
                    var asmName = new AssemblyName("PInvokeDynamic_") { Version = new Version(1, 0, 0, 0) };
                    var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                    var modBuilder = asmBuilder.DefineDynamicModule("m");
                    var typeBuilder = modBuilder.DefineType("PInvokeHolder", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

                    Type retClr = retType == "int" ? typeof(int) : (retType == "void" ? typeof(void) : typeof(int));
                    var mb = typeBuilder.DefinePInvokeMethod(funcName, dllName, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl, CallingConventions.Standard, retClr, Type.EmptyTypes, CallingConvention.Winapi, CharSet.Auto);
                    mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);
                    var createdType = typeBuilder.CreateType();
                    var mi = createdType.GetMethod(funcName, BindingFlags.Public | BindingFlags.Static);
                    if (mi == null) return new DllInvokeResult { Success = false, ErrorMessage = "动态生成的 P/Invoke 方法不存在" };

                    try
                    {
                        var r = mi.Invoke(null, null);
                        return new DllInvokeResult { Success = true, Result = r };
                    }
                    catch (TargetInvocationException tex)
                    {
                        return new DllInvokeResult { Success = false, ErrorMessage = tex.InnerException?.Message ?? tex.Message };
                    }
                }
            }
            catch (Exception ex)
            {
                return new DllInvokeResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
