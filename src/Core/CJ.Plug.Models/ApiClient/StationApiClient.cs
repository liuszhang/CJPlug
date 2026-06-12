using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.VariableType;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public class StationApiClient(HttpClient httpClient)
{
    public async Task<string> ExecuteActionTestAsync(string msg,CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetStringAsync($"/api/station/executeAction/{msg}", cancellationToken);
            
        return result;
    }

    public async Task<ExecuteResultData> StationToolExecutionAsync(PlugExecutionRequest stationExectionRequest)
    {
        // 提交到图站，等待结果或捕获异常（修复发后即忘问题）
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/station/executeToolCommand", stationExectionRequest);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new ExecuteResultData
                {
                    Ids = stationExectionRequest.ExecuteResultData.Ids,
                    ExecuteStatus = JobStatus.完成,
                    ExecuteSubStatus = JobSubStatus.出错,
                    ResultString = $"提交执行到图站失败: HTTP {(int)response.StatusCode} {response.StatusCode}, {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new ExecuteResultData
            {
                Ids = stationExectionRequest.ExecuteResultData.Ids,
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                ResultString = $"提交执行到图站失败: {ex.Message}"
            };
        }

        return new ExecuteResultData
        {
            Ids = stationExectionRequest.ExecuteResultData.Ids,
            ExecuteStatus = JobStatus.执行中,
            ExecuteSubStatus = JobSubStatus.已提交至图站,
        };
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
            ExecuteResultData.ExecuteStatus = JobStatus.完成;
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

    public async Task<bool> CheckFileExistsAsync(string filePath)
    {
        var content = new StringContent(JsonSerializer.Serialize(new { filePath }), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/station/fileExists", content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(result);
        }
        return false;
    }

    public async Task<bool> DownloadToolAsync(string toolName, string toolVersion, string targetPath, string? toolFilePath = null)
    {
        var payload = new
        {
            toolName,
            toolVersion,
            targetPath,
            toolFilePath
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/station/downloadTool", content);
        return response.IsSuccessStatusCode;
    }
}

