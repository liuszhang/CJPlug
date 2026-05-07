using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
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
            var StationToUse = await MainApiClient.GetStationToUseByTool(plugExecutionRequest.ToolName, plugExecutionRequest.ToolVersion);
            if(StationToUse == null)
            {
                CLog.Error($"未找到可用的图站来执行工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})");
                ERD.ExecuteStatus = JobStatus.完成;
                ERD.ExecuteSubStatus = JobSubStatus.出错;
                ERD.ResultString = "可使用的图站为空，请检查配置。";
                return ERD;
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
            //将获取到的工具路径根据不同图站基础地址，拼接成完整的工具执行路径
            var ToolPath = ParameterGenerator.GenerateToolPath(Tool.ToolPath, StationToUse);
            Log.Information($"工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的最终执行路径为：{ToolPath}");
            if (string.IsNullOrEmpty(ToolPath))
            {
                CLog.Error($"未找到工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行路径");
                ERD.ExecuteStatus = JobStatus.完成;
                ERD.ExecuteSubStatus = JobSubStatus.出错;
                ERD.ResultString = $"未找到工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行路径";
                return ERD;
            }
            plugExecutionRequest.ToolFullPath = ToolPath;
            //2.2 将命令行中的变量替换为参数的实际值
            plugExecutionRequest.RequestCommand = ParameterGenerator.EvalCommandLine(Command, plugExecutionRequest.InputVariables);

            if (plugExecutionRequest.RequestCommand.Contains("[ToolPath]"))
            {
                //string escapedToolPath = ToolPath.Replace(@"\", @"\\");
                string escapedToolPath = ToolPath.Replace(@"\\", Path.DirectorySeparatorChar.ToString());
                escapedToolPath = escapedToolPath.Replace(@"\", Path.DirectorySeparatorChar.ToString());
                escapedToolPath=$"\"{escapedToolPath}\"";
                plugExecutionRequest.RequestCommand = plugExecutionRequest.RequestCommand.Replace("[ToolPath]", escapedToolPath);
            }
            //Log.Information($"工具{plugExecutionRequest.ToolName}({plugExecutionRequest.ToolVersion})的执行命令为：{plugExecutionRequest.RequestCommand}");

            //3 执行工具，获取执行结果
            var result = await MainApiClient.SubmitNewToolExecute(StationToUse.StationIp, plugExecutionRequest);

            // 通知前端：图站开始执行，可用于触发 VNC 远程桌面
            StatusReporter.ReportStationExecuting(
                plugExecutionRequest.ExecuteResultData?.Ids?.PlugDefinitionId,
                StationToUse.StationIp,
                plugExecutionRequest.ExecuteResultData?.Ids?.PDZId);

            return result;

        }
    }
}
