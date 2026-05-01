using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using ExcelPlug.Utils;
using Serilog;
using System.Text.Json;

namespace ExcelPlug.Services
{
    public class ExcelPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
    {
        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
            CLog.Information($"execute Excel plug");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

            CLog.Information("开始执行Excel单元格写入");
            if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames))))
                return await ReportErrorResult(erd);

            var PlugDefinitionId = plugExecutionRequest?.PlugDefinitionId;
            var PlugData = PlugDataZone.PlugDatas?.FirstOrDefault(pd => pd.PlugDefinitionId == PlugDefinitionId);

            var TmpLocalFilePath = $"modified_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            try
            {
                var fileId = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.ExcelFile.ToString())
                    ?.GetFileIdFromFileVariable();
                if (string.IsNullOrEmpty(fileId))
                {
                    CLog.Error("未找到Excel文件", PlugDataZone?.PDZId);
                    return await ReportErrorResult(erd);
                }

                await using var sourceStream = await MainApiClient.GetFileStreamByFileId(fileId);
                (string? resultFilePath, string? resultFileId) = await MainApiClient.UploadFileStreamToWorkPathInChunks(
                    sourceStream, TmpLocalFilePath, PlugData);
                CLog.Information($"新文件路径：{resultFilePath}", PlugDataZone.PDZId);

                var textMappingString = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.TextMapping.ToString());
                if (!string.IsNullOrEmpty(textMappingString))
                {
                    var cellMappings = JsonSerializer.Deserialize<List<ExcelCellMapping>>(textMappingString) ?? new();
                    foreach (var m in cellMappings)
                    {
                        if (!string.IsNullOrEmpty(m.CellRef))
                        {
                            var cellValue = m.Value ?? "";
                            if (m.IsBoundToVariable == true && !string.IsNullOrEmpty(m.BoundVariableName))
                            {
                                var resolved = PlugDataZone?.GetVariableValue(m.BoundPlugDefinitionId, m.BoundVariableName);
                                if (!string.IsNullOrEmpty(resolved))
                                {
                                    cellValue = resolved;
                                }
                            }
                            ExcelService.SetCellValue(resultFilePath, m.CellRef, cellValue);
                        }
                    }
                }
                else
                {
                    CLog.Warning("未配置任何文本映射，直接输出源文件");
                }

                var resultVariableData = PlugDataZone.PlugVariableDatas?
                    .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                    .FirstOrDefault(p => p.Name == InitVariableNames.ResultFile.ToString());
                if (resultVariableData != null)
                {
                    var fileInfo = new FileInfo(resultFilePath);
                    resultVariableData.Value = $"{fileInfo.Name}:{resultFileId}";
                    await PDZApiClient.UpdatePlugVariableData(resultVariableData);
                }

                StatusReporter.PDZUpdated(PlugDataZone.PDZId);
            }
            catch (Exception e)
            {
                CLog.Information("Excel单元格写入出错：" + e.Message);
            }

            return await ReportCompletedResult(erd);
        }
    }
}
