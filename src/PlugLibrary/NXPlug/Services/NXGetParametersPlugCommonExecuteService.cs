using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using NXPlug;

/// <summary>
/// NX 获取模型参数插头通用执行服务。
/// 继承 <see cref="StationPlugExecuteService"/> 获得两阶段状态机（提交→图站执行完成）的通用能力。
/// </summary>
public class NXGetParametersPlugCommonExecuteService : StationPlugExecuteService
{
    public NXGetParametersPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    // ========================= 抽象实现 =========================

    protected override string ToolName => "获取NX模型参数";
    protected override string ToolVersion => "1.0";
    protected override string[]? DataPrepareVariableNames => Enum.GetNames(typeof(NXGetParametersVariables));
    protected override string ResultStringVariableName => NXGetParametersVariables.ResultString.ToString();

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.NXGetParameters.CommonExecuteKey);

    // ========================= 提交逻辑 =========================

    protected override async Task<ExecuteResultData?> SubmitAsync(PlugExecutionRequest executionRequest)
    {
        if (executionRequest.ExecuteMode == ExecuteMode.Standalone)
        {
            return await SubmitStandalone(executionRequest);
        }
        else
        {
            return await SubmitNormal(executionRequest);
        }
    }

    /// <summary>
    /// Standalone 模式：使用调用方直接传入的 InputVariables
    /// </summary>
    private async Task<ExecuteResultData?> SubmitStandalone(PlugExecutionRequest executionRequest)
    {
        return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
    }

    /// <summary>
    /// 普通模式：从 PDZ 读取变量后提交到图站执行
    /// </summary>
    private async Task<ExecuteResultData?> SubmitNormal(PlugExecutionRequest executionRequest)
    {
        var resultData = executionRequest.ExecuteResultData!;
        PDZApiClient ??= _serviceProvider.GetRequiredService<IPDZApiClient>();

        var pdzId = executionRequest.ExecuteResultData?.Ids?.PDZId;
        var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(pdzId);
        if (PDZ == null)
        {
            CLog.Error($"未找到数据空间：{pdzId}");
            resultData.ExecuteStatus = JobStatus.完成;
            resultData.ExecuteSubStatus = JobSubStatus.出错;
            return resultData;
        }

        var plugDefinitionId = resultData.Ids!.PlugDefinitionId!;
        var plugVariables = PDZ.GetVariablesOfPlug(plugDefinitionId);
        if (plugVariables != null)
        {
            foreach (var v in plugVariables)
            {
                executionRequest.InputVariables.Add(new PlugVariableData
                {
                    Name = v.Name,
                    Value = v.Value,
                    Type = v.Type,
                    IsInput = v.IsInput,
                    IsOutput = v.IsOutput,
                });
            }
        }

        return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
    }
}
