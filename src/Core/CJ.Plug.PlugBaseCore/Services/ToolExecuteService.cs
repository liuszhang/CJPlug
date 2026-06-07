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
    public class ToolExecuteService(MainApiClient MainApiClient) : IToolExecuteService
    {
        public async Task<ExecuteResultData?> ExecuteToolAsync(PlugExecutionRequest plugExecutionRequest)
        {
            var ERD = plugExecutionRequest.ExecuteResultData ?? new ExecuteResultData();


            var Tool=await MainApiClient.GetToolByDisplayNameAsync($"{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})");
            if(Tool == null)
            {
                CLog.Error($"未找到指定名称的工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})");
                ERD.ExecuteStatus = JobStatus.完成;
                ERD.ExecuteSubStatus = JobSubStatus.出错;
                ERD.ResultString = $"未找到指定名称的工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})，请检查配置。";
                return ERD;
            }
            //1 获取用于执行的图站，这一步决定了工具实际执行的路径
            // 如果用户手动指定了图站（SpecifiedStationIp），优先使用该图站
            var StationToUse = await MainApiClient.GetStationToUseByTool(plugExecutionRequest.ToolName, plugExecutionRequest.ToolVersion, plugExecutionRequest.SpecifiedStationIp);
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
            foreach (var variable in plugExecutionRequest.InputVariables)
            {
                //Log.Information($"处理变量：{variable.Name}，类型：{variable.Type}，值：{variable.Value}");
                if (variable.Type == VariableTypeEnum.File.ToString() && variable.IsInput==true)
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
            var ToolPath = ParameterGenerator.GenerateToolPath(Tool.ToolPath);
            Log.Information($"工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的服务端路径为：{ToolPath}");
            if (string.IsNullOrEmpty(ToolPath))
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
            var ToolBasePath = ParameterGenerator.GenerateToolPath(Tool.ToolBasePath);
            Log.Information($"工具{Tool.ToolName}({Tool.ToolVersion})的工具包根目录为：{ToolBasePath}");

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

            bool needDownload = deploySetting?.AlwaysDownloadToStation == true;

            if (!needDownload)
            {
                // 检查图站上是否存在工具文件（优先检查图站本地路径，其次检查服务端映射路径）
                var stationClient = new StationApiClient(new HttpClient
                {
                    BaseAddress = new Uri(StationToUse.StationIp)
                });
                bool exists = await stationClient.CheckFileExistsAsync(stationToolDir);
                if (!exists && !string.IsNullOrEmpty(ToolBasePath))
                    exists = await stationClient.CheckFileExistsAsync(ToolBasePath);
                if (!exists)
                    exists = await stationClient.CheckFileExistsAsync(ToolPath);
                needDownload = !exists;
                Log.Information(exists
                    ? $"工具{Tool.ToolName}在图站{StationToUse.StationIp}上已存在"
                    : $"工具{Tool.ToolName}在图站{StationToUse.StationIp}上不存在，需要下载");
            }

            if (needDownload)
            {
                Log.Information($"开始下载工具 {Tool.ToolName}({Tool.ToolVersion}) 到图站 {StationToUse.StationIp}...");
                var stationClient = new StationApiClient(new HttpClient
                {
                    BaseAddress = new Uri(StationToUse.StationIp)
                });
                var downloadResult = await stationClient.DownloadToolAsync(
                    Tool.ToolName!, Tool.ToolVersion!, stationToolDir, Tool.ToolBasePath ?? Tool.ToolPath);

                if (!downloadResult)
                {
                    Log.Error($"下载工具到图站{StationToUse.StationIp}失败");
                    ERD.ExecuteStatus = JobStatus.完成;
                    ERD.ExecuteSubStatus = JobSubStatus.出错;
                    ERD.ResultString = $"下载工具{Tool.ToolName}到图站失败，请检查网络连接和图站状态";
                    return ERD;
                }
                Log.Information($"工具{Tool.ToolName}已成功下载到图站: {stationToolDir}");
            }
            }
            else
            {
                Log.Information($"工具{Tool.ToolName}({Tool.ToolVersion})已设置为跳过下载至图站，直接使用现有工具路径: {ToolPath}");
            }
            // === 检查并下载工具到图站 结束 ===
            //2.2 将命令行中的变量替换为参数的实际值
            plugExecutionRequest.RequestCommand = ParameterGenerator.EvalCommandLine(Command, plugExecutionRequest.InputVariables);

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

            //3 执行工具，获取执行结果
            var result = await MainApiClient.SubmitNewToolExecute(StationToUse.StationIp, plugExecutionRequest);


            CLog.Information($"准备启动远程桌面");
            // 通知前端：图站开始执行，可用于触发 VNC 远程桌面
            //StatusReporter.ReportStationExecuting(
            //        result?.Ids?.PlugDefinitionId,
            //        StationToUse.StationIp,
            //        result?.Ids?.PDZId);
            var pdz = await MainApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.PDZId);
            var plugData = pdz?.GetPlugData(result?.Ids?.PlugDefinitionId);
            if (plugData != null)
            {
                var plugTypeKey = plugData.PlugTypeKey;
                CLog.Information($"插头result?.Ids?.PlugDefinitionId：{result?.Ids?.PlugDefinitionId}");
                CLog.Information($"插头plugExecutionRequest?.PlugType：{plugTypeKey}");
                var plug = await MainApiClient.GetRootPlugByTypeNameAsync(plugTypeKey);
                //var plug = await MainApiClient.GetPlugByDefinitionIdAsync(result?.Ids?.PlugDefinitionId);
                var plugSetting = plug?.GetPlugSetting(PlugSettingKey.SupportRemoteView.ToString());
                CLog.Information($"插头{plug?.Name}的远程支持设置SupportRemoteView={plugSetting}");
                if (plugSetting == "true")
                {
                    //CLog.Information($"插头{plug?.Name}的远程支持设置SupportRemoteView={plugSetting}");
                    StatusReporter.ReportStationExecuting(
                        result?.Ids?.PlugDefinitionId,
                        StationToUse.StationIp,
                        result?.Ids?.PDZId);
                }
            }
            else
            {
                CLog.Warning($"未找到 PlugData，PlugDefinitionId={result?.Ids?.PlugDefinitionId}，跳过远程桌面通知");
            }

            return result;

        }

    }
}
