using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Services
{
    public class ToolExecuteService(
        MainApiClient MainApiClient,
        ToolDownloadGuard DownloadGuard,
        ResilientDownloader ResilientDownloader) : IToolExecuteService
    {
        public async Task<ExecuteResultData?> ExecuteToolAsync(PlugExecutionRequest plugExecutionRequest)
        {
            var ERD = plugExecutionRequest.ExecuteResultData ?? new ExecuteResultData();

            // 1. 获取工具：优先使用预解析的工具对象（MCP Plugin 路径通过本地 DB 解析），避免内部 HTTP 调用
            Tool? Tool;
            if (plugExecutionRequest.ResolvedTool != null)
            {
                Tool = plugExecutionRequest.ResolvedTool;
                CLog.Information($"[TRACE-TOOL] 使用预解析工具: {Tool.ToolName}({Tool.ToolVersion})");
            }
            else
            {
                Tool = await MainApiClient.GetToolByDisplayNameAsync($"{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})");
            }
            if(Tool == null)
            {
                CLog.Error($"未找到指定名称的工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})");
                ERD.ExecuteStatus = JobStatus.完成;
                ERD.ExecuteSubStatus = JobSubStatus.出错;
                ERD.ResultString = $"未找到指定名称的工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})，请检查配置。";
                return ERD;
            }
            //2 获取用于执行的图站，这一步决定了工具实际执行的路径
            // 如果用户手动指定了图站（SpecifiedStationIp），优先使用该图站
            Station? StationToUse;
            if (plugExecutionRequest.ResolvedStation != null)
            {
                StationToUse = plugExecutionRequest.ResolvedStation;
                CLog.Information($"[TRACE-TOOL] 使用预解析图站: {StationToUse.StationIp}");
            }
            else
            {
                StationToUse = await MainApiClient.GetStationToUseByTool(plugExecutionRequest.ToolName, plugExecutionRequest.ToolVersion, plugExecutionRequest.SpecifiedStationIp);
            }
            if(StationToUse == null)
            {
                CLog.Error($"未找到可用的图站来执行工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})");
                ERD.ExecuteStatus = JobStatus.完成;
                ERD.ExecuteSubStatus = JobSubStatus.出错;
                ERD.ResultString = "可使用的图站为空，请检查配置。";
                return ERD;
            }
            // 确保 StationIp 是完整的 URI 格式（兼容纯 IP 地址或缺少 scheme 的情况）
            if (!string.IsNullOrEmpty(StationToUse.StationIp))
            {
                if (StationToUse.StationIp.StartsWith("://"))
                    StationToUse.StationIp = "http" + StationToUse.StationIp;
                else if (!StationToUse.StationIp.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                         !StationToUse.StationIp.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    StationToUse.StationIp = "http://" + StationToUse.StationIp;
            }
            //1.1 获取图站后，将参数中的文件参数下载至图站，并获取实际的文件路径作为后续工具执行的实际参数值
            // P2-P4: 先规范化多格式文件输入（base64 / URL → fileName:fileId），再统一下载
            StationApiClient stationApiClientForNorm = new StationApiClient(new HttpClient() { BaseAddress = new Uri(StationToUse.StationIp) });
            foreach (var variable in plugExecutionRequest.InputVariables)
            {
                if (variable.Type != VariableTypeEnum.File.ToString() || variable.IsInput != true)
                    continue;

                var val = variable.Value ?? "";

                // 已经是 "fileName:fileId" 格式（含冒号且非 URL/非 base64 前缀）→ 跳过规范化
                if (val.Contains(':') &&
                    !val.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !val.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                    !val.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 格式: "base64:<content>" → 上传到文件服务器获取 fileId
                if (val.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
                {
                    var base64Content = val["base64:".Length..];
                    var fileName = variable.Name ?? "uploaded_file";
                    var fileRef = await MainApiClient.UploadFileFromBase64(base64Content, fileName);
                    if (string.IsNullOrEmpty(fileRef))
                    {
                        CLog.Error($"处理 base64 文件参数失败: {variable.Name}");
                        ERD.ExecuteStatus = JobStatus.完成;
                        ERD.ExecuteSubStatus = JobSubStatus.出错;
                        ERD.ResultString = $"处理 base64 文件参数 {variable.Name} 失败";
                        return ERD;
                    }
                    variable.Value = fileRef;
                    Log.Information($"base64 文件已转换为: {fileRef}");
                    continue;
                }

                // 格式: "http://..." 或 "https://..." → 下载到文件服务器获取 fileId
                if (val.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    val.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var fileRef = await MainApiClient.UploadFileFromUrl(val, null);
                    if (string.IsNullOrEmpty(fileRef))
                    {
                        CLog.Error($"从 URL 下载文件参数失败: {variable.Name}");
                        ERD.ExecuteStatus = JobStatus.完成;
                        ERD.ExecuteSubStatus = JobSubStatus.出错;
                        ERD.ResultString = $"从 URL 下载文件参数 {variable.Name} 失败: {val}";
                        return ERD;
                    }
                    variable.Value = fileRef;
                    Log.Information($"URL 文件已转换为: {fileRef}");
                    continue;
                }
            }

            // 统一将 fileName:fileId 格式的文件下载到图站，替换为本地路径
            foreach (var variable in plugExecutionRequest.InputVariables)
            {
                //Log.Information($"处理变量：{variable.Name}，类型：{variable.Type}，值：{variable.Value}");
                if (variable.Type == VariableTypeEnum.File.ToString())
                {
                    //Log.Information($"开始处理文件变量{variable.Name}：{variable.Value}，准备下载文件到图站");
                    //下载文件至图站
                    StationApiClient stationApiClient = new StationApiClient(new HttpClient() { BaseAddress = new Uri(StationToUse.StationIp) });
                    var FileRealPath = await stationApiClient.DownloadFileByFileIdAsync(variable);
                    if (string.IsNullOrEmpty(FileRealPath))
                    {
                        CLog.Error($"下载文件{variable.Value}至图站{StationToUse.StationIp}失败");
                        ERD.ExecuteStatus = JobStatus.完成;
                        ERD.ExecuteSubStatus = JobSubStatus.出错;
                        ERD.ResultString = $"下载文件{variable.Value}至图站{StationToUse.StationIp}失败";
                        return ERD;
                    }
                    variable.Value = FileRealPath; //更新变量值为图站上的实际文件路径
                    Log.Information($"变量{variable.Name}的实际文件路径为：{FileRealPath}");
                }
            }

            //2 处理工具中的变量替换，生成实际的执行命令，对于toolpath这种进行特殊处理
            var Command = plugExecutionRequest.RequestCommand ?? Tool.CommandParameter;
            if(string.IsNullOrEmpty(Command))
            {
                Log.Warning($"工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行命令为空");
                ERD.ExecuteStatus = JobStatus.完成;
                ERD.ExecuteSubStatus = JobSubStatus.已完成;
                ERD.ResultString = $"工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行命令为空";
                return ERD;
            }

            //2.1 处理toolpath，根据不同图站配置获取不同的工具执行路径
            var filter = new ToolConfigFilter()
            {
                ToolId=Tool.Id,
                StationId= StationToUse.Id,
                //StationIP = StationToUse.StationIp,
                //ToolName = plugExecutionRequest.ToolName,
                //ToolVersion = plugExecutionRequest.ToolVersion
            };
            var toolPathByConfig = await MainApiClient.GetToolPathByFilter(filter);
            if (!string.IsNullOrEmpty(toolPathByConfig))
            {
                Log.Information("根据配置表更新工具路径：" + toolPathByConfig);
                Tool.ToolPath = toolPathByConfig;
            }
            //将获取到的工具路径根据不同图站基础地址，拼接成完整的工具执行路径（仅用于服务端检查）
            var resolvedToolPathForLog = ParameterGenerator.GenerateToolPath(Tool.ToolPath);
            Log.Information($"工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的服务端路径为：{resolvedToolPathForLog}");
            if (string.IsNullOrEmpty(resolvedToolPathForLog))
            {
                CLog.Error($"未找到工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行路径");
                ERD.ExecuteStatus = JobStatus.完成;
                ERD.ExecuteSubStatus = JobSubStatus.出错;
                ERD.ResultString = $"未找到工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行路径";
                return ERD;
            }
            // 发送相对路径给图站，由图站根据自身配置的 ToolsRootPath 解析实际路径
            plugExecutionRequest.ToolFullPath = Tool.ToolPath;

            // 解析工具包根目录的绝对路径
            var resolvedToolBasePathForLog = ParameterGenerator.GenerateToolPath(Tool.ToolBasePath);
            Log.Information($"工具{Tool.ToolName}({Tool.ToolVersion})的工具包根目录为：{resolvedToolBasePathForLog}");

            // === 检查并下载工具到图站 ===
            if (!Tool.SkipDownloadToStation)
            {
                // 获取工具部署设置
                var deploySetting = await MainApiClient.GetToolDeploySettingAsync(new ToolConfigFilter
                {
                    ToolId = Tool.Id,
                    StationId = StationToUse.Id
                });

                // 仅传递工具名，由图站根据自身配置的 ToolsRootPath 解析安装目录
                var stationToolDir = Tool.ToolName!;
                // 对于上传工具，ToolBasePath 格式为 Tools/{user}/{folder}，
                // 需提取用户层级以免不同用户同名工具冲突
                if (!string.IsNullOrEmpty(Tool.ToolBasePath))
                {
                    var normalizedBase = Tool.ToolBasePath.Replace('\\', '/').TrimStart('/');
                    var segments = normalizedBase.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length >= 3
                        && string.Equals(segments[0], "Tools", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(segments[1], "System", StringComparison.OrdinalIgnoreCase))
                    {
                        stationToolDir = string.Join("/", segments[1..]); // "liusz/MyTool"
                    }
                }
                var stationIp = StationToUse.StationIp;
                var toolName = Tool.ToolName!;
                var toolVersion = Tool.ToolVersion!;

                // 并发保护：同一工具同一图站只允许一个下载
                using (await DownloadGuard.AcquireAsync(stationIp, toolName, toolVersion, CancellationToken.None))
                {
                    bool needDownload = deploySetting?.AlwaysDownloadToStation == true;

                    if (!needDownload)
                    {
                        // 双重检查：锁内再次确认文件是否存在（可能已被其他请求下载完）
                        var stationClient = new StationApiClient(new HttpClient
                        {
                            BaseAddress = new Uri(stationIp)
                        });
                        bool exists = await stationClient.CheckFileExistsAsync(stationToolDir);
                        if (!exists && !string.IsNullOrEmpty(Tool.ToolBasePath))
                            exists = await stationClient.CheckFileExistsAsync(Tool.ToolBasePath);
                        if (!exists)
                            exists = await stationClient.CheckFileExistsAsync(Tool.ToolPath);
                        needDownload = !exists;
                        Log.Information(exists
                            ? $"工具{toolName}在图站{stationIp}上已存在"
                            : $"工具{toolName}在图站{stationIp}上不存在，需要下载");
                    }

                    if (needDownload)
                    {
                        Log.Information($"开始下载工具 {toolName}({toolVersion}) 到图站 {stationIp}...");

                        var stationClient = new StationApiClient(new HttpClient
                        {
                            BaseAddress = new Uri(stationIp)
                        });
                        var downloadResult = await stationClient.DownloadToolAsync(
                            toolName, toolVersion, stationToolDir, Tool.ToolBasePath ?? Tool.ToolPath);

                        if (!downloadResult)
                        {
                            Log.Error($"下载工具到图站{stationIp}失败");
                            ERD.ExecuteStatus = JobStatus.完成;
                            ERD.ExecuteSubStatus = JobSubStatus.出错;
                            ERD.ResultString = $"下载工具{toolName}到图站失败，请检查网络连接和图站状态";
                            return ERD;
                        }
                        Log.Information($"工具{toolName}已成功下载到图站: {stationToolDir}");
                        // 下载后验证：确认工具入口在图站上确实存在
                        var verifyClient = new StationApiClient(new HttpClient
                        {
                            BaseAddress = new Uri(stationIp)
                        });
                        var toolVerified = await verifyClient.CheckFileExistsAsync(stationToolDir);
                        if (!toolVerified)
                        {
                            Log.Error($"工具{toolName}下载完成但验证失败：图站上未找到 {stationToolDir}");
                            ERD.ExecuteStatus = JobStatus.完成;
                            ERD.ExecuteSubStatus = JobSubStatus.出错;
                            ERD.ResultString = $"下载工具{toolName}到图站完成但文件验证失败，请检查图站配置";
                            return ERD;
                        }
                        Log.Information($"工具{toolName}下载后验证通过: {stationToolDir}");
                    }
                }
            }
            else
            {
                // SkipDownloadToStation 时仍验证工具在图站上确实存在
                var stationClient = new StationApiClient(new HttpClient
                {
                    BaseAddress = new Uri(StationToUse.StationIp)
                });
                var toolExists = await stationClient.CheckFileExistsAsync(Tool.ToolName!);
                if (!toolExists)
                {
                    Log.Warning($"工具{Tool.ToolName}({Tool.ToolVersion})已设置跳过下载，但图站上未找到该工具，执行可能失败");
                }
                Log.Information($"工具{Tool.ToolName}({Tool.ToolVersion})已设置为跳过下载至图站，直接使用现有工具路径: {resolvedToolPathForLog}");
            }
            // === 检查并下载工具到图站 结束 ===
            //2.2 将命令行中的变量替换为参数的实际值
            CLog.Information($"[TRACE-TOOL] ToolName={plugExecutionRequest.ToolName}, ToolVersion={plugExecutionRequest.ToolVersion}");
            CLog.Information($"[TRACE-TOOL] RequestCommand={plugExecutionRequest.RequestCommand}, CommandParameter={Tool?.CommandParameter}");
            foreach (var v in plugExecutionRequest.InputVariables ?? new())
                CLog.Information($"[TRACE-TOOL] InputVariable: {v.Name}={v.Value}");
            plugExecutionRequest.RequestCommand = ParameterGenerator.EvalCommandLine(Command, plugExecutionRequest.InputVariables);
            CLog.Information($"[TRACE-TOOL] EvalCommandLine后 — RequestCommand={plugExecutionRequest.RequestCommand}");

            if (plugExecutionRequest.RequestCommand.Contains("[ToolPath]"))
            {
                // 使用原始相对路径，由图站根据自身 ToolsRootPath 解析实际执行路径
                var rawToolPath = Tool.ToolPath ?? "";
                string escapedToolPath = rawToolPath.Replace(@"\\", Path.DirectorySeparatorChar.ToString());
                escapedToolPath = escapedToolPath.Replace(@"\", Path.DirectorySeparatorChar.ToString());
                escapedToolPath=$"\"{escapedToolPath}\"";
                plugExecutionRequest.RequestCommand = plugExecutionRequest.RequestCommand.Replace("[ToolPath]", escapedToolPath);
            }
            //Log.Information($"工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行命令为：{plugExecutionRequest.RequestCommand}");

            //3.1 提前通知前端打开 VNC 远程桌面（必须在 SubmitNewToolExecute 之前发送，
            //    因为 Standalone 模式下该方法是同步等待进程结束的，事后发送就晚了）
            CLog.Information($"准备启动远程桌面");
            var vncPlugDefId = plugExecutionRequest.ExecuteResultData?.Ids?.PlugDefinitionId;
            var pdzForVnc = await MainApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.PDZId);
            var plugDataForVnc = pdzForVnc?.GetPlugData(vncPlugDefId);
            var plugForVnc = plugDataForVnc != null
                ? await MainApiClient.GetRootPlugByTypeNameAsync(plugDataForVnc.PlugTypeKey)
                : !string.IsNullOrEmpty(vncPlugDefId)
                    ? await MainApiClient.GetPlugByDefinitionIdAsync(vncPlugDefId)
                    : null;
            if (plugForVnc != null)
            {
                var plugSetting = plugForVnc.GetPlugSetting(PlugSettingKey.SupportRemoteView.ToString());
                if (plugSetting == "true")
                {
                    CLog.Information($"插头{plugForVnc.Name}已启用 SupportRemoteView，发送 StationExecuting 通知");
                    StatusReporter.ReportStationExecuting(vncPlugDefId, StationToUse.StationIp, plugExecutionRequest.PDZId);
                }
            }

            //3.2 执行工具，获取执行结果
            var result = await MainApiClient.SubmitNewToolExecute(StationToUse.StationIp, plugExecutionRequest);

            return result;

        }

    }
}
