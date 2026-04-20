using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;
using System.IO.Pipelines;
using System.Text.Json;
using TextWriterPlug;
using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.LogModels;
using CJ.Plug.FileManageApiClient;
using Microsoft.Extensions.DependencyInjection;

public class TextWriterPlugCommonExecuteService : BasePlugExecuteService
{
    private IFileManageApiClient? FileManageApiClient;

    public TextWriterPlugCommonExecuteService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        FileManageApiClient= FileManageApiClient??_serviceProvider.GetRequiredService<IFileManageApiClient>();
    }
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        string? PlugDefinitionId = context.plugExecutionRequest?.ExecuteResultData?.Ids?.PlugDefinitionId;
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

        CLog.Information($"开始执行文本解析写插头");
        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }
        var PlugData=PlugDataZone.PlugDatas?.FirstOrDefault(pd => pd.PlugDefinitionId == PlugDefinitionId);
        var OutputResults = new Dictionary<string, string>();

        try
        {
            var fileId = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.TextFile.ToString())?.GetFileIdFromFileVariable();
            if (string.IsNullOrEmpty(fileId))
            {
                CLog.Error($"未找到文件");
                return await ReportErrorResult(erd);
            }
            // 获取原始文件流
            await using var sourceStream = await FileManageApiClient.GetFileStreamByFileId(fileId);

            // 处理流
            await using var processedStream = await StreamProcesser(sourceStream,PlugDefinitionId,PlugDataZone);

            var resultFileName = $"modified_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            // 上传处理后的流
            (string? resultFilePath,string? resultFileId)=await FileManageApiClient.UploadFileStreamToWorkPathInChunks(
                processedStream,
                resultFileName,
                PlugData);
            var resultVariableData=PlugDataZone.PlugVariableDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault(p => p.Name == InitVariableNames.ResultFile.ToString());
            var fileInfo=new FileInfo(resultFilePath);
            resultVariableData.Value = $"{fileInfo.Name}:{resultFileId}";
            await PDZApiClient.UpdatePlugVariableData(resultVariableData);
            StatusReporter.PDZUpdated(PlugDataZone.PDZId);

            return await ReportCompletedResult(erd);

        }
        catch (Exception ex)
        {
            CLog.Error($"执行插头失败[text]: {ex.Message}");
            CLog.Error($"{ex.StackTrace}");
            return await ReportErrorResult(erd);
        }
    }



    private string? GenerateTextMapping(string fileContent, string plugDefinitionId, PlugDataZone plugDataZone)
    {
        if(string.IsNullOrEmpty(fileContent))
        {
            return null;
        }
        string newContent = fileContent;
        //var textMappingString = plugToExecute.GetPlugSetting(PlugSettingKey.TextMapping.ToString());
        var textMappingString = plugDataZone.GetVariableValue(plugDefinitionId, PlugSettingKey.TextMapping.ToString());
        if (string.IsNullOrEmpty(textMappingString))
        {
            Log.Warning($"未配置任何文本映射");
            //await ReportCompletedResult(erd);

            return null;
        }

        var TextMappings = JsonSerializer.Deserialize<List<TextMapping>>(textMappingString)??new();
        //执行脚本插头的处理逻辑，获取新的文件内容

        foreach (var m in TextMappings)
        {
            var inputVariableData = PlugDataZone.PlugVariableDatas?
                        .Where(p => p.PlugDefinitionId == plugDefinitionId)
                        .FirstOrDefault(p => p.Name == m.BindingVariableName);
            var Keywords = m.Keywords;
            int KeyPosition = fileContent.IndexOf(Keywords);
            if (KeyPosition != -1)
            {
                var StartOffset = KeyPosition + m.StartOffset;
                var EndOffset = KeyPosition + m.EndOffset;
                var oldContent = fileContent.Substring(StartOffset, EndOffset - StartOffset);
                newContent = fileContent.Replace(oldContent, inputVariableData?.Value ?? string.Empty);
            }
            else
            {
                //Log.Information($"未找到关键字：'{Keywords}'");
            }
        }

        return newContent;
    }



    private async Task<Stream> StreamProcesser(Stream inputStream, string plugDefinitionId, PlugDataZone plugDataZone)
    {
        var pipe = new Pipe();

        // 启动一个任务处理输入流并写入管道
        var processingTask = Task.Run(async () =>
        {
            try
            {
                using var reader = new StreamReader(inputStream);
                await using var writer = new StreamWriter(pipe.Writer.AsStream());

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    line=GenerateTextMapping(line, plugDefinitionId, plugDataZone) ?? line;
                    //if (line.Contains("END OF JOB"))
                    //{
                    //    //var parts = line.Split(':');
                    //    line = $"{line}->ADD:TEST666";
                    //}
                    //Console.WriteLine(line);
                    await writer.WriteLineAsync(line);
                }

                await writer.FlushAsync();
            }
            finally
            {
                // 完成写入
                await pipe.Writer.CompleteAsync();
            }
        });

        // 返回管道的读取流
        return pipe.Reader.AsStream();
    }


}

