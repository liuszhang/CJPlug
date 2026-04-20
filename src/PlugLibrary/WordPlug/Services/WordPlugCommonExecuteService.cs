
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
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Serilog;
using System.Text.Json;
using WordPlug;

public class WordPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
{

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);
    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        CLog.Information($"execute Word plug");
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

        CLog.Information($"开始执行文本解析写插头");
        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }
        //await DataPrepare(plugExecutionRequest);
        var PlugDefinitionId = plugExecutionRequest?.PlugDefinitionId;
        var PlugData = PlugDataZone.PlugDatas?.FirstOrDefault(pd => pd.PlugDefinitionId == PlugDefinitionId);


        var TmpLocalFilePath = $"modified_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
        //string TmpLocalFilePath = await MainApiClient.DownloadFileByFileId(PlugDataZone.GetVariablesOfPlug(PlugDefinitionId));
        string bookmarkName = "";
        string textToInsert = "";

        try
        {
            var fileId = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.WordFile.ToString())?.GetFileIdFromFileVariable();
            if (string.IsNullOrEmpty(fileId))
            {
                CLog.Error($"未找到文件");
                return await ReportErrorResult(erd);
            }
            // 获取原始文件流
            await using var sourceStream = await MainApiClient.GetFileStreamByFileId(fileId);
            // 上传流为一个新文件，获取新文件的路径
            (string? resultFilePath, string? resultFileId) = await MainApiClient.UploadFileStreamToWorkPathInChunks(
                sourceStream,
                TmpLocalFilePath,
                PlugData);
            CLog.Information($"新文件路径：{resultFilePath}",PlugDataZone.PDZId);


            var textMappingString = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.WordTextMapping.ToString());
            if (string.IsNullOrEmpty(textMappingString))
            {
                CLog.Warning($"未配置任何文本映射");
                return await ReportCompletedResult(erd);
            }

            var TextMappings = JsonSerializer.Deserialize<List<WordTextMapping>>(textMappingString) ?? new();
            //执行脚本插头的处理逻辑，获取新的文件内容

            foreach (var m in TextMappings)
            {
                bookmarkName = m.BMName;
                textToInsert = m.InputSchema;
                WordService_Aspose.InsertHtmlAtBookmark(resultFilePath, bookmarkName, textToInsert);
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
            CLog.Information("插入书签数据出错：" + e.Message);
        }

        return await ReportCompletedResult(erd);
    }

    
}

