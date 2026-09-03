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
    /// Catia 导出STL 通用执行服务。
    /// 继承 <see cref="StationPlugExecuteService"/> 获得两阶段状态机的通用能力。
    /// ToolName 与种子 ToolSeedDataProvider.DefaultTools 的「Catia模型转STL」逐字一致（对齐方案 6.2 三处一致）。
    /// </summary>
    public class CatiaExportStlCommonExecuteService : StationPlugExecuteService
    {
        public CatiaExportStlCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        // ========================= 抽象实现 =========================

        protected override string ToolName => "Catia模型转STL";
        protected override string ToolVersion => "1.0";
        protected override string[]? DataPrepareVariableNames => Enum.GetNames(typeof(CatiaExportStlVariables));
        protected override string ResultStringVariableName => CatiaExportStlVariables.ResultString.ToString();

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CatiaExportStl.CommonExecuteKey);

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
                var stlOutputPath = inputVars.FirstOrDefault(v => v.Name == "stlOutputPath")?.Value;
                var sag = inputVars.FirstOrDefault(v => v.Name == "sag")?.Value;

                if (!string.IsNullOrEmpty(modelFilePath))
                {
                    inputs.Add(new PlugVariableData
                    {
                        Name = CatiaExportStlVariables.ModelFilePath.ToString(),
                        Value = modelFilePath
                    });
                }
                if (!string.IsNullOrEmpty(stlOutputPath))
                {
                    inputs.Add(new PlugVariableData
                    {
                        Name = CatiaExportStlVariables.StlOutputPath.ToString(),
                        Value = stlOutputPath
                    });
                }
                if (!string.IsNullOrEmpty(sag))
                {
                    inputs.Add(new PlugVariableData
                    {
                        Name = CatiaExportStlVariables.Sag.ToString(),
                        Value = sag
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
                CatiaExportStlVariables.ModelFilePath.ToString(), identityId);
            if (!string.IsNullOrEmpty(modelFilePath))
            {
                executionRequest.InputVariables.Add(new PlugVariableData
                {
                    Name = CatiaExportStlVariables.ModelFilePath.ToString(),
                    Value = modelFilePath
                });
            }

            // 从 PDZ 读取 STL 输出路径
            var stlOutputPath = GetPDZVariableValue(PDZ, plugDefinitionId,
                CatiaExportStlVariables.StlOutputPath.ToString(), identityId);
            if (!string.IsNullOrEmpty(stlOutputPath))
            {
                executionRequest.InputVariables.Add(new PlugVariableData
                {
                    Name = CatiaExportStlVariables.StlOutputPath.ToString(),
                    Value = stlOutputPath
                });
            }

            // 从 PDZ 读取弦高偏差 Sag
            var sag = GetPDZVariableValue(PDZ, plugDefinitionId,
                CatiaExportStlVariables.Sag.ToString(), identityId);
            if (!string.IsNullOrEmpty(sag))
            {
                executionRequest.InputVariables.Add(new PlugVariableData
                {
                    Name = CatiaExportStlVariables.Sag.ToString(),
                    Value = sag
                });
            }

            return await ToolExecuteService!.ExecuteToolAsync(executionRequest);
        }
    }
}
