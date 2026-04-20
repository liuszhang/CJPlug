using CJ.Plug.Models.Job;
using System.Net.Http.Json;

namespace CJ.Plug.JobManageApiClient
{
    public partial class JobManageApiClient : BaseApiClient, IJobManageApiClient
    {
        public JobManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }

        public async Task<IEnumerable<BaseJob?>> GetAllJobsAsync(CancellationToken cancellationToken = default)
            {
                var result = httpClient.GetFromJsonAsAsyncEnumerable<BaseJob>("/api/Job/getJobs", cancellationToken);
                return result.ToEnumerable();
            }
            public async Task<ProcessJob?> CreateJobAsync(ProcessJob request, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.PostAsJsonAsync("/api/Job/createNewJob", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ProcessJob>(cancellationToken: cancellationToken);
            }

            /// <summary>
            /// 通过TPH模型可以调用统一的接口"/api/Job/createNewJob"传递不同的子类
            /// </summary>
            /// <param name="request"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public async Task<BaseJob?> CreateJobAsync(BaseJob request, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.PostAsJsonAsync("/api/Job/createNewJob", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BaseJob>(cancellationToken: cancellationToken);
            }

            public async Task<BaseJob?> UpdateJobAsync(BaseJob request, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.PutAsJsonAsync("/api/Job/updateJob", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BaseJob>(cancellationToken: cancellationToken);
            }
            public async Task<ProcessJob?> UpdateProcessJobAsync(ProcessJob request, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.PutAsJsonAsync("/api/Job/updateProcessJob", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ProcessJob>(cancellationToken: cancellationToken);
            }
            public async Task<bool> DeleteJobAsync(BaseJob request, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.DeleteAsync($"/api/job/deleteJob/{request.Id}", cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: cancellationToken);
            }

            public async Task<BaseJob?> GetJobByCorrelationIdAsync(string CorrelationId, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.GetFromJsonAsync<BaseJob>($"/api/Job/getJobByCorrelationId/{CorrelationId}", cancellationToken);
                return response;
            }
            public async Task<ProcessJob?> GetProcessJobByCorrelationIdAsync(string CorrelationId, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.GetFromJsonAsync<ProcessJob>($"/api/Job/getProcessJobByCorrelationId/{CorrelationId}", cancellationToken);
                return response;
            }

            public async Task<BaseJob?> GetJobByDefinitionIdAsync(string DefinitionId, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.GetFromJsonAsync<BaseJob>($"/api/Job/getJobByDefinitionId/{DefinitionId}", cancellationToken);
                return response;
            }

            public async Task<List<BaseJob>?> GetJobsByFilter(JobFilter filter, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.PostAsJsonAsync("/api/job/getJobsByFilter", filter, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<BaseJob>>(cancellationToken: cancellationToken);
            }



            //同步作业历程数据
            public async Task SyncJournalData(string JobCorrelationId, CancellationToken cancellationToken = default)
            {
                var response = await httpClient.GetAsync($"/api/Job/SyncJournalData/{JobCorrelationId}", cancellationToken);
                response.EnsureSuccessStatusCode();
                //不需要返回值
            }

        
    }
}
