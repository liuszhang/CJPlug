using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using CMDPlug;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// CMD 插头通用执行服务。
/// 继承 <see cref="StationPlugExecuteService"/> 获得两阶段状态机（提交→图站执行完成）的通用能力。
/// </summary>
public class CMDPlugCommonExecuteService : StationPlugExecuteService
{
    public CMDPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    // ========================= 抽象实现 =========================

    protected override string ToolName => "CMD";
    protected override string ToolVersion => "1.0";
    protected override string[]? DataPrepareVariableNames => Enum.GetNames(typeof(InitVariableNames));
    protected override string ResultStringVariableName => InitVariableNames.ResultString.ToString();

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

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
    /// Standalone 模式（MCP Plugin 类型）：使用 VariableResolver.ResolveStandalone() 统一解析参数。
    /// 解析链：InputVariables → PlugVariables.Value。
    /// </summary>
    private async Task<ExecuteResultData?> SubmitStandalone(PlugExecutionRequest executionRequest)
    {
        CLog.Information($"[TRACE-CMD] SubmitStandalone开始 — ExecuteMode={executionRequest.ExecuteMode}");
        CLog.Information($"[TRACE-CMD] InputVariables 数量: {executionRequest.InputVariables?.Count ?? 0}");
        foreach (var v in executionRequest.InputVariables ?? new())
            CLog.Information($"[TRACE-CMD]   {v.Name}={v.Value}");
        var inputs = new List<PlugVariableData>();
        var inputVars = executionRequest.InputVariables;
        if (inputVars != null && inputVars.Count > 0)
        {
            // 使用 VariableResolver 统一解析入口：InputVariables → PlugVariables.Value
            var cmd = VariableResolver.ResolveStandalone("command", inputVars, plugVariables: null);
            var args = VariableResolver.ResolveStandalone("arguments", inputVars, plugVariables: null);
            var workingDir = VariableResolver.ResolveStandalone("workingDirectory", inputVars, plugVariables: null);
            var timeout = VariableResolver.ResolveStandalone("timeout", inputVars, plugVariables: null);

            var cmdCommand = cmd;
            if (!string.IsNullOrEmpty(args))
                cmdCommand += " " + args;

            if (!string.IsNullOrEmpty(cmdCommand))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = InitVariableNames.CMDCommand.ToString(),
                    Value = cmdCommand
                });
            }
            if (!string.IsNullOrEmpty(workingDir))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = InitVariableNames.RedirectWorkPath.ToString(),
                    Value = workingDir
                });
            }
            if (!string.IsNullOrEmpty(timeout))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = InitVariableNames.ExecutionTimeout.ToString(),
                    Value = timeout
                });
            }
        }

        executionRequest.InputVariables.Clear();
        executionRequest.InputVariables.AddRange(inputs);
        CLog.Information($"[TRACE-CMD] ExecuteToolAsync前 — RequestCommand={executionRequest.RequestCommand}");
        foreach (var v in executionRequest.InputVariables)
            CLog.Information($"[TRACE-CMD]   最终InputVariable: {v.Name}={v.Value}");
        return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
    }

    /// <summary>
    /// 普通模式：从 PDZ 读取变量后提交到图站执行
    /// </summary>
    private async Task<ExecuteResultData?> SubmitNormal(PlugExecutionRequest executionRequest)
    {
        CLog.Information($"[TRACE-CMD] SubmitNormal开始 — ExecuteMode={executionRequest.ExecuteMode}");
        CLog.Information($"[TRACE-CMD] PDZId={executionRequest.ExecuteResultData?.Ids?.PDZId}");
        var resultData = executionRequest.ExecuteResultData!;
        PDZApiClient ??= _serviceProvider.GetRequiredService<IPDZApiClient>();

        var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(executionRequest.ExecuteResultData.Ids?.PDZId);
        if (PDZ == null)
        {
            CLog.Error($"未找到数据空间：{executionRequest.ExecuteResultData.Ids?.PDZId}");
            resultData.ExecuteStatus = JobStatus.完成;
            resultData.ExecuteSubStatus = JobSubStatus.出错;
            return resultData;
        }

        var plugDefinitionId = executionRequest.ExecuteResultData.Ids!.PlugDefinitionId!;
        var identityId = ParseIdentityId(executionRequest);

        var inputs = new List<PlugVariableData>
        {
            new()
            {
                Name = InitVariableNames.CMDCommand.ToString(),
                Value = GetCMDCommandLine(PDZ, plugDefinitionId, identityId)
            }
        };

        // 执行超时参数
        var timeoutValue = GetPDZVariableValue(PDZ, plugDefinitionId,
            InitVariableNames.ExecutionTimeout.ToString(), identityId);
        if (!string.IsNullOrEmpty(timeoutValue))
        {
            inputs.Add(new PlugVariableData
            {
                Name = InitVariableNames.ExecutionTimeout.ToString(),
                Value = timeoutValue
            });
        }

        executionRequest.InputVariables.AddRange(inputs);
        CLog.Information($"[TRACE-CMD] ExecuteToolAsync前 — RequestCommand={executionRequest.RequestCommand}");
        foreach (var v in executionRequest.InputVariables)
            CLog.Information($"[TRACE-CMD]   最终InputVariable: {v.Name}={v.Value}");
        return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
    }

    // ========================= 辅助方法 =========================

    /// <summary>
    /// 获取 PDZ 中 CMDCommand 变量的值（兼容普通变量和动作变量）
    /// </summary>
    private string? GetCMDCommandLine(PlugDataZone pdz, string plugDefinitionId, string? identityId = null)
    {
        return GetPDZVariableValue(pdz, plugDefinitionId, InitVariableNames.CMDCommand.ToString(), identityId);
    }
}
