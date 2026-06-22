using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordReadPlug;

public class WordReadPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
{
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        CLog.Information("execute WordRead plug");
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames))))
        {
            return await ReportErrorResult(erd);
        }

        var PlugDefinitionId = plugExecutionRequest?.PlugDefinitionId;
        var PlugData = PlugDataZone?.PlugDatas?.FirstOrDefault(pd => pd.PlugDefinitionId == PlugDefinitionId);

        try
        {
            var fileId = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.WordFile.ToString())?.GetFileIdFromFileVariable();
            if (string.IsNullOrEmpty(fileId))
            {
                CLog.Error("未找到Word文件", PlugDataZone?.PDZId);
                return await ReportErrorResult(erd);
            }

            var tmpLocalFilePath = $"wordread_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            using var sourceStream = await MainApiClient.GetFileStreamByFileId(fileId);
            using var wordDoc = WordprocessingDocument.Open(sourceStream, false);
            var readResult = wordDoc.MainDocumentPart!.Document.Body!.InnerText;

            var resultVariableData = PlugDataZone?.PlugVariableDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault(p => p.Name == InitVariableNames.ReadResult.ToString());

            if (resultVariableData != null)
            {
                resultVariableData.Value = readResult;
                await PDZApiClient.UpdatePlugVariableData(resultVariableData);
            }

            StatusReporter.PDZUpdated(PlugDataZone?.PDZId);

            CLog.Information("Word文件读取完成", PlugDataZone?.PDZId);
        }
        catch (Exception e)
        {
            CLog.Information("Word读取出错：" + e.Message);
            return await ReportErrorResult(erd);
        }

        return await ReportCompletedResult(erd);
    }
}
