using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using PatranPlug.Models;
using Serilog;
using System.Text.Json;

namespace PatranPlug.Services
{
    public class PatranPlugCommonExecuteService : BasePlugExecuteService
    {
        private IToolExecuteService ToolExecuteService { get; set; }
        private IFileManageApiClient? FileManageApiClient;

        public PatranPlugCommonExecuteService(IServiceProvider serviceProvider, IToolExecuteService toolExecuteService) : base(serviceProvider)
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

            if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames))))
                return await ReportErrorResult(resultData);

            CLog.Information($"开始执行Patran插头，状态为{status}", plugExecutionRequest.ExecuteResultData.Ids.PDZId);

            if (status == JobSubStatus.提交)
            {
                resultData = await SubmitPatranExecute(plugExecutionRequest);
                return await ExecuteResultReport(resultData);
            }
            else if (status == JobSubStatus.图站执行完成)
            {
                Log.Information("图站执行完成，执行Patran插头数据后处理");
                try
                {
                    PDZApiClient = PDZApiClient ?? _serviceProvider.GetRequiredService<IPDZApiClient>();
                    var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
                    var resultString = plugExecutionRequest.ExecuteResultData.ResultString;
                    var executeId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
                    var identityId = executeId.Contains("|") ? executeId.Split('|')[1] : null;

                    if (identityId == null)
                    {
                        PDZ?.SetVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId,
                            InitVariableNames.ResultString.ToString(), resultString);
                    }
                    else
                    {
                        PDZ?.SetActionVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId,
                            identityId, InitVariableNames.ResultString.ToString(), resultString);
                    }

                    await PDZApiClient.CreateOrUpdatePDZ(PDZ);
                    resultData.ExecuteStatus = JobStatus.完成;
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
        /// 提交Patran执行：读取脚本、替换关键字、上传修改后的脚本、调用工具执行
        /// </summary>
        private async Task<ExecuteResultData?> SubmitPatranExecute(PlugExecutionRequest? plugExecutionRequest)
        {
            var resultData = plugExecutionRequest.ExecuteResultData;
            FileManageApiClient = FileManageApiClient ?? _serviceProvider.GetRequiredService<IFileManageApiClient>();
            PDZApiClient = PDZApiClient ?? _serviceProvider.GetRequiredService<IPDZApiClient>();

            var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids?.PDZId);
            if (PDZ == null)
            {
                CLog.Error($"未找到数据空间：{plugExecutionRequest.ExecuteResultData.Ids?.PDZId}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return resultData;
            }

            var plugDefinitionId = plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId;

            // 1. 获取脚本文件
            var scriptFileVar = PDZ.GetVariableValue(plugDefinitionId, InitVariableNames.ScriptFile.ToString());
            var scriptFileId = scriptFileVar?.GetFileIdFromFileVariable();
            if (string.IsNullOrEmpty(scriptFileId))
            {
                CLog.Error("未找到脚本文件，请先导入Patran脚本");
                return await ReportErrorResult(resultData);
            }

            // 2. 下载脚本内容
            var scriptContent = await FileManageApiClient.GetFileContentByFileId(scriptFileId);
            if (string.IsNullOrEmpty(scriptContent))
            {
                CLog.Error("脚本文件内容为空");
                return await ReportErrorResult(resultData);
            }

            // 3. 获取关键字映射配置并执行替换
            var mappingJson = PDZ.GetVariableValue(plugDefinitionId, InitVariableNames.KeywordMappingJson.ToString());
            scriptContent = ReplaceKeywords(scriptContent, mappingJson, PDZ, plugDefinitionId);

            // 4. 上传替换后的脚本文件
            var plugData = PDZ.PlugDatas?.FirstOrDefault(pd => pd.PlugDefinitionId == plugDefinitionId);
            var resultFileName = $"patran_modified_{DateTime.Now:yyyyMMdd_HHmmss}.ses";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(scriptContent));
            (string? resultFilePath, string? resultFileId) = await FileManageApiClient.UploadFileStreamToWorkPathInChunks(
                stream, resultFileName, plugData);

            if (string.IsNullOrEmpty(resultFileId))
            {
                CLog.Error("上传修改后的脚本文件失败");
                return await ReportErrorResult(resultData);
            }

            // 5. 更新脚本文件变量为替换后的文件
            var scriptVarData = PDZ.PlugVariableDatas?
                .Where(p => p.PlugDefinitionId == plugDefinitionId)
                .FirstOrDefault(p => p.Name == InitVariableNames.ScriptFile.ToString());
            if (scriptVarData != null)
            {
                scriptVarData.Value = $"{resultFileName}:{resultFileId}";
                await PDZApiClient.UpdatePlugVariableData(scriptVarData);
            }

            // 6. 构建执行请求
            var executionRequest = plugExecutionRequest ?? new PlugExecutionRequest();
            executionRequest.ToolName = "Patran";
            executionRequest.ToolVersion = "20122";

            var inputs = new List<PlugVariableData>();

            // 添加修改后的脚本文件作为输入变量
            inputs.Add(new PlugVariableData
            {
                Name = InitVariableNames.ScriptFile.ToString(),
                Value = $"{resultFileName}:{resultFileId}"
            });

            // 添加额外命令行参数（如果有）
            var additionalArgs = PDZ.GetVariableValue(plugDefinitionId, InitVariableNames.AdditionalArgs.ToString());
            if (!string.IsNullOrEmpty(additionalArgs))
            {
                inputs.Add(new PlugVariableData
                {
                    Name = InitVariableNames.AdditionalArgs.ToString(),
                    Value = additionalArgs
                });
            }

            executionRequest.InputVariables.AddRange(inputs);

            // 7. 调用工具执行服务
            resultData = await ToolExecuteService.ExecuteToolAsync(executionRequest);
            return resultData;
        }

        /// <summary>
        /// 执行关键字替换：将脚本中的关键字替换为绑定变量的实际值
        /// </summary>
        private string ReplaceKeywords(string scriptContent, string? mappingJson, PlugDataZone pdz, string plugDefinitionId)
        {
            if (string.IsNullOrEmpty(mappingJson))
            {
                Log.Warning("未配置任何关键字映射，跳过替换");
                return scriptContent;
            }

            var mappings = JsonSerializer.Deserialize<List<PatranKeywordMapping>>(mappingJson) ?? new();
            if (mappings.Count == 0)
            {
                Log.Warning("关键字映射列表为空，跳过替换");
                return scriptContent;
            }

            string result = scriptContent;
            foreach (var m in mappings)
            {
                if (string.IsNullOrEmpty(m.Keyword))
                    continue;

                var value = pdz.GetVariableValue(plugDefinitionId, m.VariableName) ?? string.Empty;
                if (result.Contains(m.Keyword))
                {
                    result = result.Replace(m.Keyword, value);
                    Log.Information($"关键字替换: '{m.Keyword}' -> '{value}'");
                }
                else
                {
                    Log.Warning($"未在脚本中找到关键字: '{m.Keyword}'");
                }
            }

            return result;
        }
    }
}
