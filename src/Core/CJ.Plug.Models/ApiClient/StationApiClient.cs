using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.VariableType;
using System.Net.Http.Json;
using System.Text.Json;

public class StationApiClient(HttpClient httpClient)
{
    public async Task<string> ExecuteActionTestAsync(string msg,CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetStringAsync($"/api/station/executeAction/{msg}", cancellationToken);
            
        return result;
    }

    public ExecuteResultData StationToolExecutionAsync(PlugExecutionRequest stationExectionRequest)
    {
        //提交到图站，不等待结果，后续执行结果由图站执行完成后主档推送到服务端(由于引擎恢复活动目前有BUG，后续修复后再改为同步执行)
        _ =httpClient.PostAsJsonAsync($"/api/station/executeToolCommand", stationExectionRequest);
        //Console.WriteLine("11"+JsonSerializer.Serialize(result));
        //Console.WriteLine("22" + JsonSerializer.Serialize(result.Content));
        //Console.WriteLine("33" + JsonSerializer.Serialize(result.Content.ReadAsStringAsync()));
        //return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        return new ExecuteResultData
        {
            Ids = stationExectionRequest.ExecuteResultData.Ids,
            ExecuteStatus = JobStatus.执行中,
            ExecuteSubStatus = JobSubStatus.已提交至图站,
        };

        //return result;
    }

    public async Task<ExecuteResultData?> StationExecutionWaitResultAsync(PlugExecutionRequest stationExectionRequest)
    {
        //提交到图站，并等待结果
        var response = await httpClient.PostAsJsonAsync($"/api/station/executeToolCommand", stationExectionRequest);
        var result = await response.Content.ReadFromJsonAsync<ExecuteResultData>();
        return result;

    }

    public async Task SendLog(LogModel logModel)
    {
        try
        {
            var Log = new LogModel
            {
                Description = logModel.Description,
                Type = logModel.Type,
                Author = "StationApiClient",
            };
            await httpClient.PostAsJsonAsync<LogModel>("/api/station/sendLog", Log);            
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending log1: " + ex.Message);
        }
        return;
    }

    public async Task SendLog(object logString, string? logLevel = "Information")
    {
        try
        {
            var Log = new LogModel
            {
                Description = logString.ToString(),
                Type = logLevel,
                Author = "StationAgent",
            };
            await httpClient.PostAsJsonAsync<LogModel>("/api/station/sendLog", Log);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending log22: " + ex.Message);
        }
    }

    public async Task SendResult(PlugExecutionRequest ExecuteRequest, string? resultString, JobSubStatus? status)
    {
        try
        {
            foreach (var variable in ExecuteRequest.InputVariables)
            {
                if(variable.Type == VariableTypeEnum.File.ToString() && variable.IsOutput == true)
                {
                    //如果是文件类型的输出变量，上传文件至文件服务器
                    //Log.Information($"准备上传图站结果文件至文件参数：{variable.Name}({variable.Id})");
                    //Console.WriteLine($"准备上传图站结果文件至文件参数：{variable.Name}({variable.Id})");
                    var response = await httpClient.PostAsJsonAsync<PlugVariableData>("/api/station/UploadFileToVariable", variable);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Error details: " + errorContent);
                    }
                }
            }
            var ExecuteResultData = ExecuteRequest.ExecuteResultData ?? new ExecuteResultData();
            ExecuteResultData.ResultString = resultString;
            ExecuteResultData.ExecuteSubStatus = status;
            Console.WriteLine($"发送执行结果状态：{JsonSerializer.Serialize(ExecuteResultData)}");
            var result = await httpClient.PostAsJsonAsync<ExecuteResultData>("/api/station/reportResult", ExecuteResultData);
            if (!result.IsSuccessStatusCode)
            {
                var errorContent = await result.Content.ReadAsStringAsync();
                Console.WriteLine("Error details: " + errorContent);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending log2: " + ex.Message);
        }
    }


    public async Task<string> DownloadFileByFileIdAsync(PlugVariableData plugVariableData)
    {
        if(plugVariableData.Type != VariableTypeEnum.File.ToString())
        {
            CLog.Error("下载文件失败，变量类型不是文件类型。");
            return string.Empty;
        }
        var fileId = plugVariableData.Value?.GetFileIdFromFileVariable();
        var fileName = plugVariableData.Value?.GetFileNameFromFileVariable();

        var response = await httpClient.GetAsync($"/api/station/DownloadFileByFileId/{fileId}?fileName={Uri.EscapeDataString(fileName)}");
        if (response.IsSuccessStatusCode)
        {
            var filePath = await response.Content.ReadAsStringAsync();
            return filePath;
        }
        else
        {
            CLog.Error($"图站下载文件{fileId}失败，状态码：{response.StatusCode}");
            return string.Empty;
        }
    }
}

