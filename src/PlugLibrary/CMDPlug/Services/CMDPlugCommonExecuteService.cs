using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using CMDPlug;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

public class CMDPlugCommonExecuteService : BasePlugExecuteService
{
    private IToolExecuteService ToolExecuteService { get; set; }

    public CMDPlugCommonExecuteService(IServiceProvider serviceProvider, IToolExecuteService toolExecuteService) : base(serviceProvider)
    {
        ToolExecuteService = toolExecuteService;
    }

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plug = context.plugToExecute;
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

        var resultData = plugExecutionRequest.ExecuteResultData;
        var status = plugExecutionRequest.ExecuteResultData?.ExecuteSubStatus;
        CLog.Information($"开始执行CMD插头，状态为{status.ToString()}", context.plugExecutionRequest.ExecuteResultData.Ids.PDZId);
        if (status == JobSubStatus.提交)
        {
            resultData = await SubmitCmdExecute(plugExecutionRequest);
            return await ExecuteResultReport(resultData);
        }
        else if (status==JobSubStatus.图站执行完成)
        {
            Log.Information("图站执行完成，执行CMD插头数据后处理");
            //if (plugExecutionRequest.ExecuteMode==ExecuteMode.Standalone)
            //{
            //    Log.Information("独立执行模型，直接返回结果数据");
            //    resultData.ExecuteStatus = JobStatus.完成;
            //    resultData.ExecuteSubStatus = JobSubStatus.已完成;
            //    return await ExecuteResultReport(resultData);
            //}
            try
            {
                //非独立执行模式，需要更新相关PDZ数据
                //将plugExecutionRequest.ExecuteResultData的值写入相应的数据空间
                //Log.Information($"PDZ:{plugExecutionRequest.ExecuteResultData.Ids.PDZId}");
                PDZApiClient= PDZApiClient??_serviceProvider.GetRequiredService<IPDZApiClient>();
                var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
                var resultString = plugExecutionRequest.ExecuteResultData.ResultString;
                var execteId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
                var identityId = execteId.Contains("|") ? execteId.Split('|')[1] : null;
                if (identityId == null)
                {
                    PDZ?.SetVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, InitVariableNames.ResultString.ToString(), resultString);
                }
                else
                {
                    PDZ?.SetActionVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, identityId, InitVariableNames.ResultString.ToString(), resultString);                    
                }
                await PDZApiClient.CreateOrUpdatePDZ(PDZ);
                resultData.ExecuteStatus= JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.已完成;
                return await ExecuteResultReport(resultData);
            }
            catch (Exception ex)
            {
                CLog.Error($"{plug.Name}后处理执行失败：{ex.Message}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return await ((IPlugCommonExecute)this).ExecuteResultReport(MainApiClient, resultData);
            }
        }
        return await ExecuteResultReport(resultData);
    }

    /// <summary>
    /// 获取PDZ中指定插头的命令行数据
    /// </summary>
    /// <param name="plugDataZone"></param>
    /// <param name="PlugDefinitionId"></param>
    /// <param name="IdentityId"></param>
    /// <returns></returns>
    private string? GetCMDCommandLine(PlugDataZone plugDataZone,string PlugDefinitionId,string? IdentityId=null)
    {
        if (IdentityId == null)
        {
            var commandLine = plugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.CMDCommand.ToString());
            return commandLine;
        }
        else
        {
            var commandLine = plugDataZone.ActionVariableDatas?
                .Where(p => p.ActionIdentityId == IdentityId)
                .Where(p => p.Name == InitVariableNames.CMDCommand.ToString())
                .FirstOrDefault()?.Value;
            return commandLine;
        }
    }

    public async Task<ExecuteResultData?> SubmitCmdExecute(PlugExecutionRequest? plugExecutionRequest)
    {
        var resultData = plugExecutionRequest.ExecuteResultData;

        var execteId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
        //包含|则表示执行的是插头的动作
        var identityId = execteId.Contains("|") ? execteId.Split('|')[1] : null;
        var ExecutionRequest = plugExecutionRequest ?? new PlugExecutionRequest();
        //工具信息是插头特有的，CMD插头就指定调用CDM 1.0工具
        ExecutionRequest.ToolName = "CMD";
        ExecutionRequest.ToolVersion = "1.0";
        var inputs = new List<PlugVariableData>();
        if (plugExecutionRequest.ExecuteMode != ExecuteMode.Standalone)
        {
            PDZApiClient = PDZApiClient ?? _serviceProvider.GetRequiredService<IPDZApiClient>();

            var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids?.PDZId);
            if (PDZ == null)
            {
                CLog.Error($"未找到数据空间：{plugExecutionRequest.ExecuteResultData.Ids?.PDZId}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return resultData;
            }
            inputs.Add(new PlugVariableData
            {
                Name = InitVariableNames.CMDCommand.ToString(),
                Value = GetCMDCommandLine(PDZ, plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, identityId)
            });
        }
        //将插头的实际参数信息放入执行请求中（如果是独立执行模式，放的是个空的）
        ExecutionRequest.InputVariables.AddRange(inputs);
        resultData = await ToolExecuteService.ExecuteToolAsync(ExecutionRequest);
        return resultData;
    }

    


}

