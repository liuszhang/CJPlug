using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using System.Net.Http.Json;
using System.Text.Json;

namespace CJ.Plug.JobManageApiClient
{
    public partial class JobManageApiClient
    {
        
            public async Task<ToolJob?> GetToolJobByCorrelationIdAsync(string CorrelationId, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.GetFromJsonAsync<ToolJob>($"/api/Job/getJobByCorrelationId/{CorrelationId}", cancellationToken);
                return response;
            }

            public async Task<List<ToolJob>?> GetToolJobsByParentJobAsync(string ParentJobCorrelationId, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.GetFromJsonAsync<IEnumerable<ToolJob>?>($"/api/Job/getToolJobsByParentJob/{ParentJobCorrelationId}", cancellationToken);
                return response?.ToList();
            }


            public async Task<ToolJob?> UpdateToolJobAsync(ToolJob request, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.PutAsJsonAsync("/api/Job/updateToolJob", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolJob>(cancellationToken: cancellationToken);
            }

            public async Task<ExecuteResultData?> GetToolJobResultByCorrelationIdAsync(string? CorrelationId, CancellationToken cancellationToken = default)
            {
                try
                {
                    var filter = new JobFilter()
                    {
                        CorrelationId = CorrelationId
                    };
                    //var job = (await GetJobsByFilter(filter, cancellationToken))?.FirstOrDefault();
                    var job = (await GetToolJobByCorrelationIdAsync(CorrelationId, cancellationToken));
                    if (job != null)
                    {
                        //Log.Information($"获取工具作业结果: {JsonSerializer.Serialize(job)}");
                        var result = JsonSerializer.Deserialize<ExecuteResultData>(job.ExecuteResultData ?? "", new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        });
                        return result;
                    }
                    return null;

                }
                catch (Exception ex)
                {
                    CLog.Error($"获取工具作业结果失败: {ex.Message}");
                    return null;
                }

            }


            public async Task<ExecuteResultData?> SubmitNewToolExecute(string stationIp, PlugExecutionRequest request)
            {
                Console.WriteLine("start to execute:" + JsonSerializer.Serialize(request));

                StationApiClient stationApiClient = new StationApiClient(new HttpClient() { BaseAddress = new Uri(stationIp) });
                try
                {
                    //Log.Information("submited to station");
                    Console.WriteLine("submited to station");
                    request.ExecuteResultData.Ids.ToolJobCorrelationId = request.ExecuteResultData.Ids.ToolJobCorrelationId ?? RandomLongIdentityGenerator.GenerateId();

                    //创建工具作业，用于保存和管理工具执行过程的数据
                    var toolJob = await CreateToolJobAsync(new ToolJob
                    {
                        ToolName = request.ToolName,
                        ToolVersion = request.ToolVersion,
                        StationIp = stationIp,
                        JobDefinitionId = request.ExecuteResultData.Ids.ToolJobCorrelationId,
                        JobCorrelationId = request.ExecuteResultData.Ids.ToolJobCorrelationId,
                        ParentJobCorrelationId = request.ExecuteResultData.Ids.JobCorrelationId,
                        JobCategory = JobCategoryEnum.ToolJob.ToString(),
                        //ExecuteResultData = JsonSerializer.Serialize(result),
                    });
                    //独立执行模式，不创建作业，直接等待执行完成的结果
                    if (request.ExecuteMode == ExecuteMode.Standalone)
                    {
                        request.ExecuteResultData.Ids.JobCorrelationId = null;
                        request.ExecuteResultData.Ids.ToolJobCorrelationId = null;
                        return await stationApiClient.StationExecutionWaitResultAsync(request);
                    }
                    //非独立执行模式，只执行提交，不等待结果完成

                    var result = stationApiClient.StationToolExecutionAsync(request);
                    return result;
                }
                catch (Exception ex)
                {
                    CLog.Error("error:" + ex.Message);
                    return new ExecuteResultData
                    {
                        Ids = request.ExecuteResultData.Ids,
                        ExecuteStatus = JobStatus.完成,
                        ExecuteSubStatus = JobSubStatus.出错,
                        ResultString = ex.Message
                    };
                }
            }

            public async Task<ToolJob?> CreateToolJobAsync(ToolJob request, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.PostAsJsonAsync("/api/Job/createToolJob", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ToolJob>(cancellationToken: cancellationToken);
            }

        


    }
}
