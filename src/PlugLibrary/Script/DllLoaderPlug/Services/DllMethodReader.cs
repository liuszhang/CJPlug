using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DllLoaderPlug.Services
{
    public class DllMethodReader
    {
        private static readonly object _sync = new object();
        private static Task<List<MethodDescriptor>?>? _ongoingTask;

        // Read method metadata from a PE file without loading the assembly into the runtime.
        // This avoids triggering type initializers or other runtime behavior that can cause stack overflows.
        public static Task<List<MethodDescriptor>?> DllReader(string? dllPath)
        {
            //_ongoingTask = Task.Run(() => ReadInternal(dllPath));
            //return _ongoingTask;

            lock (_sync)
            {
                if (_ongoingTask != null)
                {
                    return _ongoingTask;
                }

                _ongoingTask = Task.Run(() => ReadInternal(dllPath));
                return _ongoingTask;
            }
        }

        private static List<MethodDescriptor>? ReadInternal(string? dllPath)
        {
            try
            {
                if (OperatingSystem.IsBrowser())
                {
                    Console.WriteLine("运行在浏览器环境，无法加载本地 DLL。请在服务器/桌面环境执行。");
                    return new List<MethodDescriptor>();
                }

                // If no path provided, do not default to a system DLL (previously msedge.dll).
                // Require the caller to supply a DLL path (e.g. via file upload) so we can inspect the intended assembly.
                if (string.IsNullOrEmpty(dllPath))
                {
                    Console.WriteLine("未指定 DLL 路径。请上传或指定要解析的 .NET 程序集文件路径（.dll）。");
                    return new List<MethodDescriptor>();
                }

                if (!File.Exists(dllPath))
                {
                    Console.WriteLine($"错误：未找到指定的DLL文件（路径：{dllPath}）");
                    return new List<MethodDescriptor>();
                }

                using var fs = File.OpenRead(dllPath);
                using var peReader = new PEReader(fs);

                if (peReader.HasMetadata)
                {
                    var mr = peReader.GetMetadataReader();
                    var result = new List<MethodDescriptor>();

                    foreach (var typeHandle in mr.TypeDefinitions)
                    {
                        var typeDef = mr.GetTypeDefinition(typeHandle);
                        var name = mr.GetString(typeDef.Name);
                        var ns = mr.GetString(typeDef.Namespace);
                        if (string.IsNullOrEmpty(name)) continue;

                        var fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
                        if (fullName.StartsWith("System.")) continue;

                        if ((typeDef.Attributes & TypeAttributes.Interface) != 0) continue;

                        foreach (var methodHandle in typeDef.GetMethods())
                        {
                            var methodDef = mr.GetMethodDefinition(methodHandle);
                            var methodName = mr.GetString(methodDef.Name);
                            if (string.IsNullOrEmpty(methodName)) continue;
                            if (methodName.StartsWith("get_") || methodName.StartsWith("set_") || methodName == ".ctor") continue;

                            var md = new MethodDescriptor
                            {
                                DeclaringType = fullName,
                                Name = methodName,
                                ReturnType = "(unknown)",
                                IsStatic = (methodDef.Attributes & MethodAttributes.Static) != 0,
                                AccessModifier = GetAccessModifierFromMethodAttributes(methodDef.Attributes)
                            };

                            md.SourcePath = dllPath;

                            foreach (var pHandle in methodDef.GetParameters())
                            {
                                var p = mr.GetParameter(pHandle);
                                var pname = mr.GetString(p.Name);
                                md.Parameters.Add(new ParameterDescriptor { Name = pname, TypeName = "?" });
                            }

                            result.Add(md);
                        }
                    }

                    return result;
                }
                else
                {
                    // Native/unmanaged DLL: try to read export table to list exported functions
                    var peHeaders = peReader.PEHeaders;
                    var exportDir = peHeaders.PEHeader.ExportTableDirectory;
                    if (exportDir.RelativeVirtualAddress == 0)
                    {
                        Console.WriteLine("该文件不是托管 .NET 程序集，且未导出任何符号（或导出表为空）。");
                        return null;
                    }

                    // helper to convert RVA to file offset
                    long RvaToOffset(int rva)
                    {
                        var section = peHeaders.SectionHeaders.FirstOrDefault(s => s.VirtualAddress <= rva && rva < s.VirtualAddress + s.VirtualSize);
                        // SectionHeader is a struct; if not found its VirtualSize will be 0
                        if (section.VirtualSize == 0) return -1;
                        return (long)(rva - section.VirtualAddress + section.PointerToRawData);
                    }

                    long exportOffset = RvaToOffset(exportDir.RelativeVirtualAddress);
                    if (exportOffset < 0)
                    {
                        Console.WriteLine("无法解析导出表偏移。");
                        return null;
                    }

                    using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);
                    fs.Seek(exportOffset, SeekOrigin.Begin);

                    // Read IMAGE_EXPORT_DIRECTORY
                    // struct fields: Characteristics(4), TimeDateStamp(4), MajorVersion(2), MinorVersion(2), Name(4), Base(4), NumberOfFunctions(4), NumberOfNames(4), AddressOfFunctions(4), AddressOfNames(4), AddressOfNameOrdinals(4)
                    uint characteristics = br.ReadUInt32();
                    uint timeDateStamp = br.ReadUInt32();
                    ushort major = br.ReadUInt16();
                    ushort minor = br.ReadUInt16();
                    uint nameRva = br.ReadUInt32();
                    uint baseVal = br.ReadUInt32();
                    uint numberOfFunctions = br.ReadUInt32();
                    uint numberOfNames = br.ReadUInt32();
                    uint addressOfFunctions = br.ReadUInt32();
                    uint addressOfNames = br.ReadUInt32();
                    uint addressOfNameOrdinals = br.ReadUInt32();

                    var exports = new List<MethodDescriptor>();

                    // First try to read named exports
                    for (uint i = 0; i < numberOfNames; i++)
                    {
                        // read name RVA
                        long namePtrOffset = RvaToOffset((int)(addressOfNames + i * 4));
                        if (namePtrOffset < 0) continue;
                        fs.Seek(namePtrOffset, SeekOrigin.Begin);
                        uint nRva = br.ReadUInt32();

                        long nameOffset = RvaToOffset((int)nRva);
                        if (nameOffset < 0) continue;
                        fs.Seek(nameOffset, SeekOrigin.Begin);

                        // read null-terminated ASCII
                        var nameBytes = new List<byte>();
                        int b;
                        while ((b = fs.ReadByte()) > 0)
                        {
                            nameBytes.Add((byte)b);
                        }
                        var exportName = Encoding.ASCII.GetString(nameBytes.ToArray());

                        // read ordinal
                        long ordOffset = RvaToOffset((int)(addressOfNameOrdinals + i * 2));
                        if (ordOffset < 0) continue;
                        fs.Seek(ordOffset, SeekOrigin.Begin);
                        ushort ordinal = br.ReadUInt16();

                        exports.Add(new MethodDescriptor
                        {
                            DeclaringType = Path.GetFileName(dllPath),
                            Name = exportName,
                            ReturnType = "native export",
                            IsStatic = true,
                            AccessModifier = "native",
                            Parameters = new List<ParameterDescriptor>()
                            ,
                            SourcePath = dllPath
                        });
                    }

                    // If no named exports found, try to enumerate by ordinal only
                    if (exports.Count == 0 && numberOfFunctions > 0)
                    {
                        long funcsOffset = RvaToOffset((int)addressOfFunctions);
                        if (funcsOffset >= 0)
                        {
                            fs.Seek(funcsOffset, SeekOrigin.Begin);
                            for (uint i = 0; i < numberOfFunctions; i++)
                            {
                                uint fRva = br.ReadUInt32();
                                // name as ordinal
                                exports.Add(new MethodDescriptor
                                {
                                    DeclaringType = Path.GetFileName(dllPath),
                                    Name = $"ordinal_{baseVal + i}",
                                    ReturnType = "native export",
                                    IsStatic = true,
                                    AccessModifier = "native",
                                    Parameters = new List<ParameterDescriptor>()
                                ,
                                SourcePath = dllPath
                                });
                            }
                        }
                    }

                    if (exports.Count == 0)
                    {
                        Console.WriteLine("该文件不是托管 .NET 程序集，且未导出任何可识别的符号（或导出表为空）。");
                        return new List<MethodDescriptor>();
                    }

                    return exports;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常：{ex.Message}");
                return new List<MethodDescriptor>();
            }
            finally
            {
                // ensure the cached task is cleared so subsequent calls will create a fresh task
                lock (_sync)
                {
                    _ongoingTask = null;
                }
            }
        }

        private static string GetAccessModifierFromMethodAttributes(MethodAttributes attrs)
        {
            if ((attrs & MethodAttributes.Public) != 0) return "public";
            if ((attrs & MethodAttributes.Private) != 0) return "private";
            if ((attrs & MethodAttributes.Family) != 0) return "protected";
            if ((attrs & MethodAttributes.Assembly) != 0) return "internal";
            if ((attrs & MethodAttributes.FamORAssem) != 0) return "protected internal";
            return "unknown";
        }
    }

    public class MethodDescriptor
    {
        public string? DeclaringType { get; set; }
        public string? Name { get; set; }
        public string? ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public string? AccessModifier { get; set; }
        public string? SourcePath { get; set; }
        public List<ParameterDescriptor> Parameters { get; set; } = new();
    }

    public class ParameterDescriptor
    {
        public string? Name { get; set; }
        public string? TypeName { get; set; }
    }
}
