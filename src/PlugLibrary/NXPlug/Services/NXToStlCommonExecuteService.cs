using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using NXPlug;

/// <summary>
/// NX模型转STL 通用执行服务。
/// 继承 <see cref="StationPlugExecuteService"/> 获得两阶段状态机的通用能力。
/// </summary>
public class NXToStlCommonExecuteService : StationPlugExecuteService
{
    public NXToStlCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    // ========================= 抽象实现 =========================

    protected override string ToolName => "NXToStl";
    protected override string ToolVersion => "1.0";
    protected override string[]? DataPrepareVariableNames => Enum.GetNames(typeof(NXToStlVariables));
    protected override string ResultStringVariableName => NXToStlVariables.ResultString.ToString();

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.NXToStl.CommonExecuteKey);

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
    /// Standalone 模式（MCP Plugin 类型）：从 InputVariables 读取 MCP 传入的参数
    /// </summary>
    private async Task<ExecuteResultData?> SubmitStandalone(PlugExecutionRequest executionRequest)
    {
        var inputs = new List<PlugVariableData>();
        var inputVars = executionRequest.InputVariables;
        if (inputVars != null && inputVars.Count > 0)
        {
            var prtPath = inputVars.FirstOrDefault(v => v.Name == "prtFilePath")?.Value;
            var stlPath = inputVars.FirstOrDefault(v => v.Name == "stlOutputPath")?.Value;
            var chordalTol = inputVars.FirstOrDefault(v => v.Name == "chordalTol")?.Value;
            var adjacencyTol = inputVars.FirstOrDefault(v => v.Name == "adjacencyTol")?.Value;
            var autoNormalGen = inputVars.FirstOrDefault(v => v.Name == "autoNormalGen")?.Value;

            if (!string.IsNullOrEmpty(prtPath))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = NXToStlVariables.PrtFilePath.ToString(),
                    Value = prtPath
                });
            }
            if (!string.IsNullOrEmpty(stlPath))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = NXToStlVariables.StlOutputPath.ToString(),
                    Value = stlPath
                });
            }
            if (!string.IsNullOrEmpty(chordalTol))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = NXToStlVariables.ChordalTol.ToString(),
                    Value = chordalTol
                });
            }
            if (!string.IsNullOrEmpty(adjacencyTol))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = NXToStlVariables.AdjacencyTol.ToString(),
                    Value = adjacencyTol
                });
            }
            if (!string.IsNullOrEmpty(autoNormalGen))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = NXToStlVariables.AutoNormalGen.ToString(),
                    Value = autoNormalGen
                });
            }
        }

        executionRequest.InputVariables.Clear();
        executionRequest.InputVariables.AddRange(inputs);
        return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
    }

    /// <summary>
    /// 普通模式：从 PDZ 读取变量后提交到图站执行
    /// </summary>
    private async Task<ExecuteResultData?> SubmitNormal(PlugExecutionRequest executionRequest)
    {
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

        // 从 PDZ 读取 PRT 文件路径
        var prtFilePath = GetPDZVariableValue(PDZ, plugDefinitionId,
            NXToStlVariables.PrtFilePath.ToString(), identityId);
        if (!string.IsNullOrEmpty(prtFilePath))
        {
            executionRequest.InputVariables.Add(new PlugVariableData
            {
                Name = NXToStlVariables.PrtFilePath.ToString(),
                Value = prtFilePath
            });
        }

        // 从 PDZ 读取 STL 输出路径
        var stlOutputPath = GetPDZVariableValue(PDZ, plugDefinitionId,
            NXToStlVariables.StlOutputPath.ToString(), identityId);
        if (!string.IsNullOrEmpty(stlOutputPath))
        {
            executionRequest.InputVariables.Add(new PlugVariableData
            {
                Name = NXToStlVariables.StlOutputPath.ToString(),
                Value = stlOutputPath
            });
        }

        // 读取公差等参数
        var chordalTol = GetPDZVariableValue(PDZ, plugDefinitionId,
            NXToStlVariables.ChordalTol.ToString(), identityId);
        if (!string.IsNullOrEmpty(chordalTol))
        {
            executionRequest.InputVariables.Add(new PlugVariableData
            {
                Name = NXToStlVariables.ChordalTol.ToString(),
                Value = chordalTol
            });
        }

        var adjacencyTol = GetPDZVariableValue(PDZ, plugDefinitionId,
            NXToStlVariables.AdjacencyTol.ToString(), identityId);
        if (!string.IsNullOrEmpty(adjacencyTol))
        {
            executionRequest.InputVariables.Add(new PlugVariableData
            {
                Name = NXToStlVariables.AdjacencyTol.ToString(),
                Value = adjacencyTol
            });
        }

        var autoNormalGen = GetPDZVariableValue(PDZ, plugDefinitionId,
            NXToStlVariables.AutoNormalGen.ToString(), identityId);
        if (!string.IsNullOrEmpty(autoNormalGen))
        {
            executionRequest.InputVariables.Add(new PlugVariableData
            {
                Name = NXToStlVariables.AutoNormalGen.ToString(),
                Value = autoNormalGen
            });
        }

        return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
    }
}
