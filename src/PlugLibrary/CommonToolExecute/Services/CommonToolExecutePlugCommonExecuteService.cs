using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.StationAndToolApiClient;
using CommonToolExecute;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json;

public class CommonToolExecutePlugCommonExecuteService: BasePlugExecuteService
{
    private readonly IToolExecuteService ToolExecuteService;
    private IStationAndToolApiClient? StationAndToolApiClient;

    public CommonToolExecutePlugCommonExecuteService(IServiceProvider serviceProvider, IToolExecuteService toolExecuteService) : base(serviceProvider)
    {
        ToolExecuteService = toolExecuteService;
        PDZApiClient = PDZApiClient ?? _serviceProvider.GetRequiredService<IPDZApiClient>();
        StationAndToolApiClient= StationAndToolApiClient??_serviceProvider.GetRequiredService<IStationAndToolApiClient>();
    }

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plugToExecute = context.plugToExecute;
        
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

        Log.Information($"开始执行插头: {plugToExecute.Name}");

        var resultData = new ExecuteResultData() 
        { 
            Ids = plugExecutionRequest.ExecuteResultData.Ids,
            ExecuteStatus = JobStatus.执行中,
            ExecuteSubStatus = JobSubStatus.执行中
        };
        var status = plugExecutionRequest.ExecuteResultData?.ExecuteSubStatus;

        if (status == JobSubStatus.提交)
        {
            var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids?.PDZId);
            if (PDZ == null)
            {
                CLog.Error($"未找到数据空间：{plugExecutionRequest.ExecuteResultData.Ids?.PDZId}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return await ((IPlugCommonExecute)this).ExecuteResultReport(MainApiClient, resultData);
            }

            var execteId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
            var identityId = execteId.Contains("|") ? execteId.Split('|')[1] : null;

            //Log.Information($"EvaledCommandLine: {EvaledCommandLine}");

            var ExecutionRequest = await GenerateRequest(plugToExecute,PDZ) ?? new PlugExecutionRequest();
            var subResult = await SubmitToolExecute(ExecutionRequest) ?? resultData;         
            return await ((IPlugCommonExecute)this).ExecuteResultReport(MainApiClient, subResult);
        }
        else if (status == JobSubStatus.图站执行完成)
        {
            Log.Information("图站执行完成，执行CMD插头数据后处理");
            try
            {
                //后处理-----------------------------
                //将plugExecutionRequest.ExecuteResultData的值写入相应的数据空间
                //Log.Information($"PDZ:{plugExecutionRequest.ExecuteResultData.Ids.PDZId}");
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
                    var VariableExist = PDZ?.PDZVariables
                        .Where(p => p.ActionIdentityId == identityId)
                        .FirstOrDefault(p => p.Name == InitVariableNames.ResultString.ToString());
                    if (VariableExist != null)
                    {
                        VariableExist.Value = resultString;
                    }
                    else
                    {
                        PDZ?.PDZVariables.Add(new PDZVariable()
                        {
                            Tag = PDZVariableTagEnum.ActionVariable.ToString(),
                            PlugDefinitionId = plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId,
                            ActionIdentityId = identityId,
                            Name = InitVariableNames.ResultString.ToString(),
                            Value = resultString
                        });
                    }
                }
                await PDZApiClient.CreateOrUpdatePDZ(PDZ);
                StatusReporter.PDZUpdated(PDZ.PDZId);
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.已完成;
                return await ((IPlugCommonExecute)this).ExecuteResultReport(MainApiClient, resultData);
            }
            catch (Exception ex)
            {
                CLog.Error($"{plugToExecute.Name}后处理执行失败：{ex.Message}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return await ((IPlugCommonExecute)this).ExecuteResultReport(MainApiClient, resultData);
            }
        }

        return await ((IPlugCommonExecute)this).ExecuteResultReport(MainApiClient, resultData);

    }


    private async Task<PlugExecutionRequest?> GenerateRequest(Plug Plug, PlugDataZone? PDZ)
    {
        try
        {
            var PlugVariablesInPDZ = PDZ?.PDZVariables?.Where(x => x.PlugDefinitionId == Plug?.DefinitionId).ToList() ?? new();

            var ToolVariable = PlugVariablesInPDZ.Where(v => v.Name == InitVariableNames.Tool.ToString()).FirstOrDefault();
            var tmpToolValue = JsonSerializer.Deserialize<ToolVariable>(ToolVariable?.Value);
            //Log.Information(tmpTool?.ToolId?.ToString());
            var Tool = await StationAndToolApiClient.GetToolByIdAsync(tmpToolValue.ToolId);
            if (Tool == null)
            {
                Log.Information("请先选择一个工具");
                return null;
            }

            var ToolCommandVariableData = PlugVariablesInPDZ?.Where(v => v.Name == InitVariableNames.ToolCommandVariable.ToString()).FirstOrDefault();
            var ToolCommandVariables = ToolCommandVariableData?.Value != null
                ? JsonSerializer.Deserialize<List<ToolCommandVariable>>(ToolCommandVariableData?.Value) ?? new List<ToolCommandVariable>()
                : new List<ToolCommandVariable>();

            var inputs = new List<PlugVariableData>();

            if (ToolCommandVariables != null && ToolCommandVariables.Count > 0)
            {
                foreach (var toolCommandVariable in ToolCommandVariables)
                {
                    var tmpValue = toolCommandVariable.VariableValue;
                    if (toolCommandVariable.IsFromPlugVariable)
                    {
                        tmpValue = PDZ.GetVariableValue(Plug.DefinitionId, toolCommandVariable.VariableValue?.TrimStart('[')?.TrimEnd(']'));
                    }

                    inputs.Add(new PlugVariableData
                    {
                        Name = toolCommandVariable.VariableName,
                        Value = tmpValue,
                    });
                }
            }

            var executeRequest = new PlugExecutionRequest
            {
                ToolName = Tool.ToolName,
                ToolVersion = Tool.ToolVersion,
                ExecuteMode = ExecuteMode.Standalone,
                InputVariables = inputs,
            };
            return executeRequest;
        }
        catch (Exception ex)
        {
            CLog.Error(ex.Message);
            return null;
        }
    }

    private async Task<ExecuteResultData?> SubmitToolExecute(PlugExecutionRequest executeRequest)
    {
        try
        {
            var result = await ToolExecuteService.ExecuteToolAsync(executeRequest);
            return result;
            //Log.Information($"提交结果: {JsonSerializer.Serialize(result)}");
        }
        catch (Exception ex)
        {
            CLog.Error($"提交失败: {ex.Message}");
            return new ExecuteResultData
            {
                Ids = executeRequest.ExecuteResultData.Ids,
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                ResultString = ex.Message
            };
        }
    }


}

