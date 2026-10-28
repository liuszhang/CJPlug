using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using CatiaPlug;

namespace CatiaPlug.Services
{
    /// <summary>
    /// Catia 设置模型参数插头通用执行服务。
    /// ToolName 与种子 ToolSeedDataProvider.DefaultTools 的「设置Catia模型参数」逐字一致（对齐方案 6.2 三处一致）。
    /// </summary>
    public class CatiaSetParametersCommonExecuteService : StationPlugExecuteService
    {
        public CatiaSetParametersCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        // ========================= 抽象实现 =========================

        protected override string ToolName => "设置Catia模型参数";
        protected override string ToolVersion => "1.0";
        protected override string[]? DataPrepareVariableNames => Enum.GetNames(typeof(CatiaSetParametersVariables));
        protected override string ResultStringVariableName => CatiaSetParametersVariables.ResultString.ToString();

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CatiaSetParameters.CommonExecuteKey);

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
                var modelFilePath = inputVars.FirstOrDefault(v => v.Name == "modelFilePath")?.Value;
                var newParameterString = inputVars.FirstOrDefault(v => v.Name == "newParameterString")?.Value;

                if (!string.IsNullOrEmpty(modelFilePath))
                {
                    inputs.Add(new PlugVariableData
                    {
                        Name = CatiaSetParametersVariables.ModelFilePath.ToString(),
                        Value = modelFilePath
                    });
                }
                if (!string.IsNullOrEmpty(newParameterString))
                {
                    inputs.Add(new PlugVariableData
                    {
                        Name = CatiaSetParametersVariables.NewParameterString.ToString(),
                        Value = newParameterString
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

            var inputs = new List<PlugVariableData>
            {
                new()
                {
                    Name = CatiaSetParametersVariables.ModelFilePath.ToString(),
                    Value = GetModelFilePath(PDZ, plugDefinitionId, identityId)
                },
                new()
                {
                    Name = CatiaSetParametersVariables.NewParameterString.ToString(),
                    Value = GetNewParameterString(PDZ, plugDefinitionId, identityId)
                }
            };

            executionRequest.InputVariables.AddRange(inputs);
            return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
        }

        // ========================= 辅助方法 =========================

        /// <summary>
        /// 获取 PDZ 中 ModelFilePath 变量的值
        /// </summary>
        private string? GetModelFilePath(PlugDataZone pdz, string plugDefinitionId, string? identityId = null)
        {
            return GetPDZVariableValue(pdz, plugDefinitionId, CatiaSetParametersVariables.ModelFilePath.ToString(), identityId);
        }

        /// <summary>
        /// 获取 PDZ 中 NewParameterString 变量的值
        /// </summary>
        private string? GetNewParameterString(PlugDataZone pdz, string plugDefinitionId, string? identityId = null)
        {
            return GetPDZVariableValue(pdz, plugDefinitionId, CatiaSetParametersVariables.NewParameterString.ToString(), identityId);
        }
    }
}
