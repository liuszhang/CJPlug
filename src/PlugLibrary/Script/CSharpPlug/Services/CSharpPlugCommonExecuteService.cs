using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json;

namespace CSharpPlug.Services
{
    public class CSharpPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
    {

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"execute csharp plug");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
            if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }

            var plugDefinitionId = erd.Ids.PlugDefinitionId;

            var code = PlugDataZone?.GetVariableValue(plugDefinitionId, InitVariableNames.Script.ToString());
            if (string.IsNullOrWhiteSpace(code))
            {
                Log.Warning("CSharp 代码为空，使用默认示例代码");
                code = "return \"未编写代码\";";
            }

            var dllPaths = new List<string>();
            var dllRefsJson = PlugDataZone?.GetVariableValue(plugDefinitionId, InitVariableNames.DllReferences.ToString());
            if (!string.IsNullOrWhiteSpace(dllRefsJson))
            {
                try
                {
                    var dllRefs = JsonSerializer.Deserialize<List<DllReference>>(dllRefsJson);
                    if (dllRefs != null)
                    {
                        foreach (var dllRef in dllRefs)
                        {
                            if (!string.IsNullOrEmpty(dllRef.LocalPath) && File.Exists(dllRef.LocalPath))
                            {
                                dllPaths.Add(dllRef.LocalPath);
                            }
                            else if (!string.IsNullOrEmpty(dllRef.FileId))
                            {
                                var downloadedPath = await DownloadDll(dllRef);
                                if (!string.IsNullOrEmpty(downloadedPath))
                                {
                                    dllPaths.Add(downloadedPath);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"解析 DLL 引用失败: {ex.Message}");
                }
            }

            var useDotNetFramework = PlugDataZone?.GetVariableValue(plugDefinitionId, InitVariableNames.UseDotNetFramework.ToString());
            var useBridge = string.Equals(useDotNetFramework, "true", StringComparison.OrdinalIgnoreCase);

            // 读取环境变量配置
            var envVars = new Dictionary<string, string>();
            var envVarsJson = PlugDataZone?.GetVariableValue(plugDefinitionId, InitVariableNames.EnvironmentVariables.ToString());
            if (!string.IsNullOrWhiteSpace(envVarsJson))
            {
                try
                {
                    var vars = JsonSerializer.Deserialize<List<EnvVariableEntry>>(envVarsJson);
                    if (vars != null)
                    {
                        foreach (var v in vars)
                        {
                            if (!string.IsNullOrWhiteSpace(v.Key))
                                envVars[v.Key] = v.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"解析环境变量失败: {ex.Message}");
                }
            }

            string result;
            if (useBridge)
            {
                Log.Information("CSharp 使用 .NET Framework 桥接执行（优先工具调度）");
                // 传递 serviceProvider 和相关 ID，启用工具调度系统调用
                result = await CodeRunner.RunCSharpCodeViaBridgeAsync(
                    code, dllPaths, envVars,
                    serviceProvider: _serviceProvider,
                    pdzId: erd.Ids.PDZId,
                    plugDefinitionId: plugDefinitionId,
                    correlationId: erd.Ids.JobCorrelationId);
            }
            else
            {
                result = await CodeRunner.RunCSharpCodeWithDllsAsync(code, dllPaths, envVars);
            }

            Log.Information($"CSharp 执行结果: {result}");

            erd.ExecuteStatus = JobStatus.完成;
            erd.ExecuteSubStatus = JobSubStatus.已完成;
            erd.Outcome = [InitOutcomes.结束.ToString()];
            return await ExecuteResultReport(erd);
        }

        private async Task<string?> DownloadDll(DllReference dllRef)
        {
            try
            {
                var fileApiClient = _serviceProvider.GetService(typeof(IFileManageApiClient)) as IFileManageApiClient;
                if (fileApiClient == null) return null;

                var tempDir = Path.Combine(Path.GetTempPath(), "CSharpPlug_Dlls", Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, dllRef.FileName ?? "dependency.dll");

                var stream = await fileApiClient.DownloadFileByFileId(dllRef.FileId!);
                if (stream != null)
                {
                    await using (stream)
                    await using (var fs = File.Create(tempPath))
                    {
                        await stream.CopyToAsync(fs);
                    }
                    return tempPath;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"下载 DLL 失败: {dllRef.FileName}, 错误: {ex.Message}");
            }
            return null;
        }

        public class DllReference
        {
            public string? FileName { get; set; }
            public string? FileId { get; set; }
            public string? LocalPath { get; set; }
        }

        public class EnvVariableEntry
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}
