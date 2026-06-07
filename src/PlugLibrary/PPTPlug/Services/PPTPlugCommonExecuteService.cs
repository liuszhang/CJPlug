using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using PPTPlug.Utils;
using Serilog;
using System.Text.Json;

namespace PPTPlug.Services
{
    public class PPTPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
    {
        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
            CLog.Information($"execute PPT plug");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

            CLog.Information($"开始执行PPT文本替换插头");
            if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }

            var PlugDefinitionId = plugExecutionRequest?.PlugDefinitionId;
            var PlugData = PlugDataZone.PlugDatas?.FirstOrDefault(pd => pd.PlugDefinitionId == PlugDefinitionId);

            var TmpLocalFilePath = $"modified_{DateTime.Now:yyyyMMdd_HHmmss}.pptx";

            try
            {
                var fileId = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.PPTFile.ToString())?.GetFileIdFromFileVariable();
                if (string.IsNullOrEmpty(fileId))
                {
                    CLog.Error($"未找到文件", PlugDataZone?.PDZId);
                    return await ReportErrorResult(erd);
                }

                // 获取原始文件流
                await using var sourceStream = await MainApiClient.GetFileStreamByFileId(fileId);
                // 上传流为一个新文件，获取新文件的路径
                (string? resultFilePath, string? resultFileId) = await MainApiClient.UploadFileStreamToWorkPathInChunks(
                    sourceStream,
                    TmpLocalFilePath,
                    PlugData);
                CLog.Information($"新文件路径：{resultFilePath}", PlugDataZone.PDZId);

                var textMappingString = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.PPTTextMapping.ToString());
                if (string.IsNullOrEmpty(textMappingString))
                {
                    CLog.Warning($"未配置任何文本映射");
                    return await ReportCompletedResult(erd);
                }

                var TextMappings = JsonSerializer.Deserialize<List<WordTextMapping>>(textMappingString) ?? new();

                // 构建替换字典
                var replaceDict = new Dictionary<string, string>();
                foreach (var m in TextMappings)
                {
                    if (!string.IsNullOrEmpty(m.BMName) && !string.IsNullOrEmpty(m.InputSchema))
                    {
                        replaceDict[m.BMName] = m.InputSchema;
                    }
                }

                // 执行批量文本替换
                if (replaceDict.Count > 0)
                {
                    PPTService_Aspose.BatchReplaceText(resultFilePath, replaceDict);
                }

                var resultVariableData = PlugDataZone.PlugVariableDatas?
                    .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                    .FirstOrDefault(p => p.Name == InitVariableNames.ResultFile.ToString());
                var fileInfo = new FileInfo(resultFilePath);
                resultVariableData.Value = $"{fileInfo.Name}:{resultFileId}";

                await PDZApiClient.UpdatePlugVariableData(resultVariableData);
                StatusReporter.PDZUpdated(PlugDataZone.PDZId);
            }
            catch (Exception e)
            {
                CLog.Information("PPT文本替换出错：" + e.Message);
                return await ReportErrorResult(erd);
            }

            return await ReportCompletedResult(erd);
        }
    }
}
