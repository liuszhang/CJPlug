
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Serilog;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TextReaderPlug;
using CJ.Plug.Models.Extensions;
using static MudBlazor.Colors;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.LogModels;

public class TextReaderPlugCommonExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
{

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {

        //Plug plugToExecute = context.plugToExecute;
        
        string? PlugDefinitionId = context.plugExecutionRequest?.ExecuteResultData?.Ids?.PlugDefinitionId;
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();

        if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }
        CLog.Information($"开始执行文本解析读插头", PlugDataZone?.PDZId);


        var OutputResults = new Dictionary<string, string>();

        try
        {
            //var fileId = plugToExecute.GetVariableValue(InitVariableNames.TextFile.ToString())?.Split(':')?.Last();
            var fileId = PlugDataZone?.GetVariableValue(PlugDefinitionId, InitVariableNames.TextFile.ToString())?.GetFileIdFromFileVariable();
            if (string.IsNullOrEmpty(fileId))
            {
                CLog.Error($"未找到文件",PlugDataZone?.PDZId);
                return await ReportErrorResult(erd);
            }
            var fileContent = await MainApiClient.GetFileContentByFileId(fileId);
            string newContent = fileContent;

            var textMappingString = PlugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.TextMapping.ToString());
            if (string.IsNullOrEmpty(textMappingString))
            {
                Log.Warning($"未配置任何文本映射");
                return await ReportCompletedResult(erd);
            }
            var TextMappings = JsonSerializer.Deserialize<List<TextMapping>>(textMappingString);


            //执行脚本插头的处理逻辑，获取新的文件内容
            if (!String.IsNullOrEmpty(fileContent))
            {
                foreach (var m in TextMappings)
                {
                    var outputVariableData = PlugDataZone.PlugVariableDatas?
                        .Where(p=>p.PlugDefinitionId==PlugDefinitionId)
                        .FirstOrDefault(p=>p.Name==m.BindingVariableName);
                    //MyCon1sole.WriteLine($"------<{m.Keywords}>------");
                    var Keywords = m.Keywords;
                    //MyCon1sole.WriteLine("关键字为：" + Keywords);
                    int KeyPosition = fileContent.IndexOf(Keywords);
                    //MyCon1sole.WriteLine($"------<{KeyPosition}>------");
                    if (KeyPosition != -1)
                    {
                        //MyCon1sole.WriteLine($"字符串'{Keywords}'首次出现在位置: {KeyPosition}");
                        var StartOffset = KeyPosition + m.StartOffset;
                        var EndOffset = KeyPosition + m.EndOffset;
                        //MyCon1sole.WriteLine("原始值为：" + fileContent);
                        //MyCon1sole.WriteLine("替换值为："+Input[m.InputName]);
                        //MyCon1sole.WriteLine("起始位置：" + StartOffset);
                        //MyCon1sole.WriteLine("结束位置：" + EndOffset);
                        newContent = fileContent.Substring(StartOffset, EndOffset - StartOffset);
                        //MyCon1sole.WriteLine(m.OutputName+":"+newContent);
                        OutputResults.Add(m.BindingVariableName, newContent);
                        //plugToExecute.PlugVariables.Where(v => v.Name == m.OutputName).First().Value = newContent;
                        //await MainApiClient.UpdatePlugAsync(plugToExecute.Id, plugToExecute);
                        outputVariableData.Value = newContent;
                        await MainApiClient.UpdatePlugVariableData(outputVariableData);
                        //通知前端以刷新页面
                        StatusReporter.PDZUpdated(PlugDataZone.PDZId);
                        //PlugDataZone.SetVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, m.OutputName, newContent);

                        //Log.Information(newContent);
                    }
                    else
                    {
                        Log.Information($"未找到关键字：'{Keywords}'");
                    }
                }
                //await MainApiClient.CreateOrUpdatePDZ(PDZ);
                //StatusReporter.PDZUpdated(PDZ.PDZId);
                //Log.Information($"更新数据空间：{plugExecutionRequest.Ids?.PDZId}，插头ID：{plugExecutionRequest.Ids?.PlugDefinitionId}");
                //Log.Information($"数据：{PDZ.GetVariableValue(plugExecutionRequest.Ids.PlugDefinitionId, "p1")}");
                CLog.Information("解析结果:" + JsonSerializer.Serialize(OutputResults),PlugDataZone.PDZId);

                //return JsonSerializer.Serialize(OutputResults);

            }

            //无需提交到图站，直接在活动内就执行执行结果报告，用以恢复流程
            erd.ResultString = JsonSerializer.Serialize(OutputResults);
            return await ReportCompletedResult(erd);
            //return await ((IPlugCommonExecute)this).ExecuteResultReport(
            //    MainApiClient,
            //    plugToExecute,
            //    plugExecutionRequest,
            //    OutputResults,
            //    JobStatus.完成,
            //    JobSubStatus.已完成);

        }
        catch (Exception ex)
        {
            CLog.Error($"执行插头失败[text]: {ex.Message}");
            return await ReportErrorResult(erd);
        }
    }

}

