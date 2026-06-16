using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;

using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugDataZoneApiClient;
using Elsa.Api.Client.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Services
{
    public class StationCategoryExecuteMethod
    {
        public static async Task<ExecuteResultData?> Execute(
            Plug.Models.Plug.Plug plug,
            PlugExecutionRequest? plugExecutionRequest,
            IPDZApiClient MainApiClient,
            IToolExecuteService ToolExecuteService)
        {
            var resultData = new ExecuteResultData() { Ids = plugExecutionRequest.ExecuteResultData.Ids };
            var status = plugExecutionRequest.ExecuteResultData?.ExecuteSubStatus;
            Log.Information($"准备执行的阶段：{status.ToString()}");

            if (status == JobSubStatus.提交 || string.IsNullOrEmpty(status?.ToString()))
            {
                plugExecutionRequest.ToolName= string.IsNullOrEmpty(plugExecutionRequest.ToolName) ? plug.ToolName: plugExecutionRequest.ToolName;
                plugExecutionRequest.ToolVersion = string.IsNullOrEmpty(plugExecutionRequest.ToolVersion) ? plug.ToolVersion : plugExecutionRequest.ToolVersion;
                plugExecutionRequest.RequestCommand = string.IsNullOrEmpty(plugExecutionRequest.RequestCommand) ? plug.ToolCommandLineShema : plugExecutionRequest.RequestCommand;

                // 上传文件夹工具（uploaded://）：构造合成 Tool，复用 ToolExecuteService 管线
                if (plug.ToolVersionPath?.StartsWith("uploaded://") == true
                    && plugExecutionRequest.ResolvedTool == null)
                {
                    var syntheticTool = BuildUploadedTool(plug);
                    if (syntheticTool != null)
                    {
                        plugExecutionRequest.ResolvedTool = syntheticTool;
                        // 使用文件夹名作为 ToolName，确保图站能按名查找本地目录
                        plugExecutionRequest.ToolName = syntheticTool.ToolName;
                        plugExecutionRequest.ToolVersion = syntheticTool.ToolVersion ?? "1.0";
                        CLog.Information($"[UPLOADED-TOOL] 合成 Tool: {syntheticTool.ToolName}, BasePath={syntheticTool.ToolBasePath}");
                    }
                    else
                    {
                        CLog.Error($"[UPLOADED-TOOL] 无法构造合成 Tool，ToolVersionPath={plug.ToolVersionPath}");
                        resultData.ExecuteStatus = JobStatus.完成;
                        resultData.ExecuteSubStatus = JobSubStatus.出错;
                        resultData.ResultString = "上传文件夹的工具路径为旧格式，请在插头管理中重新编辑并保存此插头以更新路径信息。";
                        return resultData;
                    }
                }

                // 手动输入路径的桌面类插头：构造合成 Tool 绕过 API 查找
                // 此类插头没有 ToolId，路径由用户手动指定（存储在 plug.Value 中）
                if (!plug.ToolId.HasValue
                    && !string.IsNullOrEmpty(plug.Value)
                    && !(plug.ToolVersionPath?.StartsWith("uploaded://") ?? false)
                    && plugExecutionRequest.ResolvedTool == null)
                {
                    var syntheticTool = new Tool
                    {
                        ToolName = plug.ToolName ?? "自定义工具",
                        ToolVersion = "1.0",
                        ToolPath = plug.Value,
                        CommandParameter = plug.ToolCommandLineShema,
                        SkipDownloadToStation = true,
                        IsBrowsable = false,
                    };
                    plugExecutionRequest.ResolvedTool = syntheticTool;
                    plugExecutionRequest.ToolName = syntheticTool.ToolName;
                    plugExecutionRequest.ToolVersion = syntheticTool.ToolVersion;
                    CLog.Information($"[MANUAL-PATH] 桌面插头手动路径模式，合成 Tool: Name={syntheticTool.ToolName}, Path={plug.Value}");
                }

                if(plugExecutionRequest.InputVariables.Count==0&&!string.IsNullOrEmpty(plugExecutionRequest.ExecuteResultData.Ids.PDZId))
                {
                    //这里应该使用PDZ的参数
                    var PDZ = await MainApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
                    var plugVariables = PDZ?.GetVariablesOfPlug(plug.DefinitionId);
                    plugVariables?.ForEach(p =>
                    {
                            plugExecutionRequest.InputVariables.Add(new()
                            {
                                Id = p.Id,
                                Name = p.Name,
                                Value = VariableResolver.ResolveFromPDZ(p.Name, PDZ, plug.DefinitionId),
                                Type = p.Type,
                                IsInput= p.IsInput,
                                IsOutput = p.IsOutput,
                                PlugDefinitionId = p.PlugDefinitionId,
                                PlugDataZoneId = p.PlugDataZoneId,
                            });                        
                    });
                }
                Log.Information("[TRACE-CMD] ExecuteToolAsync前 — RequestCommand={RequestCommand}", plugExecutionRequest.RequestCommand);
                resultData = await ToolExecuteService.ExecuteToolAsync(plugExecutionRequest);
                return resultData;
            }
            else if (status == JobSubStatus.图站执行完成)
            {
                Log.Information("图站执行完成，执行数据后处理");
                try
                {
                    //后处理-----------------------------
                    //将plugExecutionRequest.ExecuteResultData的值写入相应的数据空间
                    //Log.Information($"PDZ:{plugExecutionRequest.ExecuteResultData.Ids.PDZId}");
                    if (string.IsNullOrEmpty(plugExecutionRequest?.ExecuteResultData?.Ids?.PDZId))
                    {
                        Log.Information("无PDZ信息，无需更新PDZ");
                        resultData.ExecuteStatus = JobStatus.完成;
                        resultData.ExecuteSubStatus = JobSubStatus.已完成;
                        return resultData;
                    }
                    Log.Information("开始将数据回写至PDZ");
                    var PDZ = await MainApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
                    var resultString = plugExecutionRequest.ExecuteResultData.ResultString;
                    var execteId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
                    var identityId = execteId.Contains("|") ? execteId.Split('|')[1] : null;
                    var plugDefinitionId = plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId;
                    if (identityId == null)
                    {
                        PDZ?.SetVariableValue(plugDefinitionId, "ResultString", resultString);
                    }
                    else
                    {
                        PDZ.SetActionVariableValue(plugDefinitionId, identityId, "ResultString", resultString);
                    }
                    await MainApiClient.CreateOrUpdatePDZ(PDZ);
                    resultData.ExecuteStatus = JobStatus.完成;
                    resultData.ExecuteSubStatus = JobSubStatus.已完成;
                    return resultData;
                }
                catch (Exception ex)
                {
                    CLog.Error($"{plug.Name}后处理执行失败：{ex.Message}");
                    resultData.ExecuteStatus = JobStatus.完成;
                    resultData.ExecuteSubStatus = JobSubStatus.出错;
                    return resultData;
                }
            }
            return resultData;

        }

        /// <summary>
        /// 从 uploaded:// 插头配置构造合成 Tool 对象。
        /// uploaded://{userOrSystem}/{folderName}
        /// </summary>
        private static Tool? BuildUploadedTool(Plug.Models.Plug.Plug plug)
        {
            // 解析 uploaded://{userOrSystem}/{folderName}
            var pathPart = plug.ToolVersionPath!["uploaded://".Length..];
            var parts = pathPart.Split('/');
            string userOrSystem, folderName;
            string subPath;
            if (parts.Length >= 2)
            {
                // 新格式: uploaded://{userOrSystem}/{folderName}[/subPath...]
                userOrSystem = parts[0];
                folderName = parts[1];
                subPath = parts.Length > 2 ? string.Join("/", parts.Skip(2)) : "";
            }
            else
            {
                // 旧格式: uploaded://{DefinitionId}，从 Plug 元数据推导 userOrSystem 和 folderName
                Log.Warning("[UPLOADED-TOOL] 旧格式 ToolVersionPath={Path}，从 Plug 元数据推导路径", plug.ToolVersionPath);
                userOrSystem = string.Equals(plug.CreateType, PlugCreateTypeEnum.SystemInitPlug.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? "0System" : (plug.Creater ?? "unknown");
                folderName = plug.ToolName ?? parts[0];
                // 如果 ToolName 是默认值（"上传的工具"/"unknown"），无法确定真实文件夹名，需要重新保存插头
                if (string.IsNullOrEmpty(folderName) || folderName == "上传的工具" || folderName == "unknown")
                {
                    Log.Error("[UPLOADED-TOOL] 旧格式无法推导文件夹名（ToolName={ToolName}），请在插头管理中重新编辑保存此插头", folderName);
                    return null;
                }
                subPath = "";
            }

            // 从 ToolCommandLineShema 或 PlugSettings 提取入口文件
            // 新格式: entryFile 存于 PlugSettings["UploadedEntryFile"]
            // 旧格式兼容: [ToolPath]\entryFile.exe
            var cmdLine = plug.ToolCommandLineShema ?? "";
            var entryFile = plug.GetPlugSetting("UploadedEntryFile");
            if (string.IsNullOrEmpty(entryFile))
            {
                var entryMatch = Regex.Match(cmdLine, @"\[ToolPath\]\\([^\s\]]+)");
                entryFile = entryMatch.Success ? entryMatch.Groups[1].Value : "";
            }

            var toolBasePath = $"Tools/{userOrSystem}/{folderName}";
            if (!string.IsNullOrEmpty(subPath))
                toolBasePath += "/" + subPath;

            // ToolPath = 完整 exe 路径（基础目录 + 入口文件），[ToolPath] 直接替换为可执行文件路径
            var toolPath = string.IsNullOrEmpty(entryFile)
                ? toolBasePath
                : $"{toolBasePath}/{entryFile}";

            Log.Information("[UPLOADED-TOOL] 合成 Tool: Name={ToolName}, BasePath={BasePath}, ToolPath={ToolPath}, EntryFile={EntryFile}",
                folderName, toolBasePath, toolPath, entryFile);

            return new Tool
            {
                // 使用文件夹名而非默认 "上传的工具"，图站用此查找本地目录
                ToolName = folderName,
                ToolVersion = "1.0",
                ToolPath = toolPath,
                ToolBasePath = toolBasePath,
                CommandParameter = plug.ToolCommandLineShema,
                SkipDownloadToStation = false,
                IsBrowsable = false,
            };
        }


    }


}
