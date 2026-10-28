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
    /// Catia 导出STP 通用执行服务。
    /// 继承 <see cref="StationPlugExecuteService"/> 获得两阶段状态机的通用能力。
    /// ToolName 与种子 ToolSeedDataProvider.DefaultTools 的「Catia模型转STP」逐字一致（对齐方案 6.2 三处一致）。
    /// </summary>
    public class CatiaExportStpCommonExecuteService : StationPlugExecuteService
    {
        public CatiaExportStpCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        // ========================= 抽象实现 =========================

        protected override string ToolName => "Catia模型转STP";
        protected override string ToolVersion => "1.0";
        protected override string[]? DataPrepareVariableNames => Enum.GetNames(typeof(CatiaExportStpVariables));
        protected override string ResultStringVariableName => CatiaExportStpVariables.ResultString.ToString();

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CatiaExportStp.CommonExecuteKey);

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
                var stpOutputPath = inputVars.FirstOrDefault(v => v.Name == "stpOutputPath")?.Value;

                if (!string.IsNullOrEmpty(modelFilePath))
                {
                    inputs.Add(new PlugVariableData
                    {
                        Name = CatiaExportStpVariables.ModelFilePath.ToString(),
                        Value = modelFilePath
                    });
                }
                if (!string.IsNullOrEmpty(stpOutputPath))
                {
                    inputs.Add(new PlugVariableData
                    {
                        Name = CatiaExportStpVariables.StpOutputPath.ToString(),
                        Value = stpOutputPath
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

            // 从 PDZ 读取模型文件路径
            var modelFilePath = GetPDZVariableValue(PDZ, plugDefinitionId,
                CatiaExportStpVariables.ModelFilePath.ToString(), identityId);
            if (!string.IsNullOrEmpty(modelFilePath))
            {
                executionRequest.InputVariables.Add(new PlugVariableData
                {
                    Name = CatiaExportStpVariables.ModelFilePath.ToString(),
                    Value = modelFilePath
                });
            }

            // 从 PDZ 读取 STP 输出路径
            var stpOutputPath = GetPDZVariableValue(PDZ, plugDefinitionId,
                CatiaExportStpVariables.StpOutputPath.ToString(), identityId);
            if (!string.IsNullOrEmpty(stpOutputPath))
            {
                executionRequest.InputVariables.Add(new PlugVariableData
                {
                    Name = CatiaExportStpVariables.StpOutputPath.ToString(),
                    Value = stpOutputPath
                });
            }

            return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
        }
    }
}
