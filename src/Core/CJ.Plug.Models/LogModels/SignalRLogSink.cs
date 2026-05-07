using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog.Core;
using Serilog.Events;
using System.Text.Json;


public class SignalRLogSink : ILogEventSink
{
    private HubConnection _hubConnection;
    private string _loggerName;

    public SignalRLogSink(string loggerName)
    {
        _loggerName = loggerName;
        _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{GlobalData.MainDispatcherServer}/mainHub")
                .Build();
        _hubConnection.StartAsync();
    }

    public async void Emit(LogEvent logEvent)
    {
        // 1. 获取日志事件的自定义属性
        var renderedMessage = logEvent.RenderMessage().ToString();
        string receiverId = null;
        string logType=null;
        if (logEvent.Properties.TryGetValue(LogContextEnum.Receiver.ToString(), out var receiverValue))
        {
            receiverId = ExtractValue(receiverValue);
        }
        if (logEvent.Properties.TryGetValue(LogContextEnum.LogType.ToString(), out var typeValue))
        {
            logType = ExtractValue(typeValue);
        }
        if (string.IsNullOrEmpty(logType))
            logType = LogTypeEnum.CommonLog.ToString();
        //Console.WriteLine($">>>>>>>>>>>>>Log Type is:{logType}<<<<<<<<<<<<<<<<");
        //Console.WriteLine(JsonSerializer.Serialize(logEvent));
        try
        {
            LogModel logModel = new LogModel();
            //logModel.Date=logEvent.Timestamp.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            logModel.Date=logEvent.Timestamp.DateTime.ToString("HH:mm:ss.fff");
            logModel.Type=logEvent.Level.ToString();
            logModel.Author = _loggerName;
            logModel.Description = renderedMessage;

            if (_hubConnection.State != HubConnectionState.Connected)
                await _hubConnection.StartAsync();

            switch (logType)
            {
                case nameof(LogTypeEnum.ActivityStatusNow):
                    await SendActivityStatusNow(logEvent);
                    break;

                case nameof(LogTypeEnum.CompleteActivityContext):
                    await SendCompleteActivityContext(logEvent);
                    break;
                case nameof(LogTypeEnum.PDZUpdatedInfo):
                    await SendPDZUpdatedInfo(logEvent);
                    break;
                case nameof(LogTypeEnum.PlugUpdated):
                    await SendPlugUpdated(logEvent);
                    break;
                case nameof(LogTypeEnum.JobStatusUpdated):
                    await SendJobStatusUpdated(logEvent);
                    break;
                case nameof(LogTypeEnum.StationExecuting):
                    await SendStationExecuting(logEvent);
                    break;
                case nameof(LogTypeEnum.CommonLog):
                    await _hubConnection.InvokeAsync(logType, receiverId, JsonSerializer.Serialize(logModel));
                    break;

                default:
                    await _hubConnection.InvokeAsync(LogTypeEnum.CommonLog.ToString(), receiverId, JsonSerializer.Serialize(logModel));
                    break;
            }


            //if (renderedMessage.StartsWith(LogTypeEnum.ActivityStatusNow.ToString()))
            //{
            //    Console.WriteLine($"prepare to log ActivityStatusNow:{renderedMessage}");
            //    //活动运行状态日志
            //    var PDZId = renderedMessage.Split("|")[1];
            //    var DefinitionId = renderedMessage.Split("|")[2];
            //    var status = renderedMessage.Split("|")[3];
            //    if(string.IsNullOrEmpty(PDZId) || string.IsNullOrEmpty(DefinitionId) || string.IsNullOrEmpty(status)){  return;}
            //    if (_hubConnection.State != HubConnectionState.Connected)
            //        await _hubConnection.StartAsync();
            //    await _hubConnection.InvokeAsync(HubEventNameEnum.ActivityStatusNow.ToString(), PDZId,DefinitionId,status);
            //}
            //else if (renderedMessage.StartsWith("ResumeElsaProcess"))
            //{
            //    if (_hubConnection.State != HubConnectionState.Connected)
            //        await _hubConnection.StartAsync();
            //    //刺激引擎从书签继续执行流程
            //    await _hubConnection.InvokeAsync("SendResumeElsaProcess", renderedMessage);
            //}
            //else if (renderedMessage.StartsWith("CompleteActivityContext:"))
            //{
            //    Console.WriteLine($"prepare to log CompleteActivityContext 1:{renderedMessage}");
            //    if (_hubConnection.State != HubConnectionState.Connected)
            //        await _hubConnection.StartAsync();
            //    //刺激引擎从书签继续执行流程
            //    await _hubConnection.InvokeAsync("CompleteActivityContext", renderedMessage);
            //}
            //else if (renderedMessage.StartsWith("PDZUpdatedInfo"))
            //{
            //    Console.WriteLine($"prepare to log PDZUpdatedInfo:{renderedMessage}");
            //    if (_hubConnection.State != HubConnectionState.Connected)
            //        await _hubConnection.StartAsync();
            //    await _hubConnection.InvokeAsync("PDZUpdatedInfo", renderedMessage);
            //}
            //else if (renderedMessage.StartsWith("PlugUpdated:"))
            //{
            //    Console.WriteLine($"prepare to log PlugUpdated:{renderedMessage.Split(":")[1]}");
            //    if (_hubConnection.State != HubConnectionState.Connected)
            //        await _hubConnection.StartAsync();
            //    await _hubConnection.InvokeAsync("PlugUpdated", renderedMessage.Split(":")[1]);
            //}
            //else if (renderedMessage.StartsWith(HubEventNameEnum.JobStatusUpdated.ToString()))
            //{
            //    Console.WriteLine($"prepare to log JobStatusUpdated:{renderedMessage.Split(":")[1]}");
            //    if (_hubConnection.State != HubConnectionState.Connected)
            //        await _hubConnection.StartAsync();
            //    await _hubConnection.InvokeAsync(HubEventNameEnum.JobStatusUpdated.ToString(), renderedMessage.Split(":")[1]);
            //}
            //else
            //{
            //    //Console.WriteLine($"prepare to log commonlog:{renderedMessage}");

            //    if (_hubConnection.State != HubConnectionState.Connected)
            //        await _hubConnection.StartAsync();
            //    //普通日志
            //    if(!renderedMessage.Contains(":"))
            //    {
            //        await _hubConnection.InvokeAsync(HubEventNameEnum.CommonLog.ToString(), "", JsonSerializer.Serialize(logModel));
            //        return;
            //    }
            //    else
            //    {
            //        logModel.Description = renderedMessage.Split(":")[1];
            //        await _hubConnection.InvokeAsync(HubEventNameEnum.CommonLog.ToString(), renderedMessage.Split(":")[0], JsonSerializer.Serialize(logModel));
            //    }
            //}
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
        }

    }


    // 提取参数值（处理基本类型）
    private string ExtractValue(LogEventPropertyValue? value)
    {
        if (value is ScalarValue scalar && scalar.Value != null)
        {
            return scalar.Value.ToString();
        }
        return string.Empty;
    }

    private async Task SendActivityStatusNow(LogEvent logEvent)
    {
        logEvent.Properties.TryGetValue(LogContextEnum.Receiver.ToString(), out var receiverValue);        
        var PDZId = ExtractValue(receiverValue);
        logEvent.Properties.TryGetValue(LogContextEnum.PlugDefinitionId.ToString(), out var PlugDefinitionIdValue);
        var PlugDefinitionId = ExtractValue(PlugDefinitionIdValue);

        var status = logEvent.RenderMessage().ToString();
        Console.WriteLine($"prepare to log ActivityStatusNow:{status}({PDZId})");

        await _hubConnection.InvokeAsync(LogTypeEnum.ActivityStatusNow.ToString(), PDZId, PlugDefinitionId, status);

    }
    private async Task SendCompleteActivityContext(LogEvent logEvent)
    {
        var status = logEvent.RenderMessage().ToString();
        Console.WriteLine($"prepare to log CompleteActivityContext:{status}");

        await _hubConnection.InvokeAsync(LogTypeEnum.CompleteActivityContext.ToString(), status);

    }
    private async Task SendPDZUpdatedInfo(LogEvent logEvent)
    {
        var status = logEvent.RenderMessage().ToString();
        Console.WriteLine($"prepare to log PDZUpdatedInfo:{status}");

        await _hubConnection.InvokeAsync(LogTypeEnum.PDZUpdatedInfo.ToString(), status);

    }
    private async Task SendPlugUpdated(LogEvent logEvent)
    {
        var status = logEvent.RenderMessage().ToString();
        Console.WriteLine($"prepare to log PlugUpdated:{status}");

        await _hubConnection.InvokeAsync(LogTypeEnum.PlugUpdated.ToString(), status);

    }
    private async Task SendJobStatusUpdated(LogEvent logEvent)
    {
        var status = logEvent.RenderMessage().ToString();
        Console.WriteLine($"prepare to log JobStatusUpdated:{status}");

        await _hubConnection.InvokeAsync(LogTypeEnum.JobStatusUpdated.ToString(), status);

    }
    private async Task SendStationExecuting(LogEvent logEvent)
    {
        logEvent.Properties.TryGetValue(LogContextEnum.Receiver.ToString(), out var receiverValue);
        var PDZId = ExtractValue(receiverValue);
        logEvent.Properties.TryGetValue(LogContextEnum.PlugDefinitionId.ToString(), out var plugDefValue);
        var PlugDefinitionId = ExtractValue(plugDefValue);

        // StationIp is inside the JSON data in the message
        var rendered = logEvent.RenderMessage().ToString();
        string StationIp = "";
        try
        {
            var info = System.Text.Json.JsonSerializer.Deserialize<StationExecutingData>(rendered);
            StationIp = info?.StationIp ?? "";
        }
        catch { StationIp = rendered; }

        Console.WriteLine($"prepare to log StationExecuting:{PlugDefinitionId} on {StationIp}");
        await _hubConnection.InvokeAsync(LogTypeEnum.StationExecuting.ToString(), PDZId, PlugDefinitionId, StationIp);
    }

    private class StationExecutingData
    {
        public string? PlugDefinitionId { get; set; }
        public string? StationIp { get; set; }
    }

}

