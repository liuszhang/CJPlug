
using Elsa.Api.Client.Shared.Models;
using Serilog;
using Elsa.Workflows.Runtime;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using CJ.Plug.ElsaIntegration.Contracts;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa.Workflows.Runtime.Filters;




namespace CJ.Plug.ElsaIntegration.Services
{
    public class ElsaEngineService : IElsaEngineService
    {
        private IBookmarkResumer BookmarkResumer { get; set; }
        

        public ElsaEngineService(           
            IBookmarkResumer bookmarkResumer
            )
        {
            BookmarkResumer = bookmarkResumer;


            //在构造函数中登录到引擎，无需每次调用时都登录
            //LoginToElsaEngine();
        }


        public async Task<WorkflowInstanceSummary?> GetWorkflowInstanceByCorrelationId(string CorrelationId)
        {
            var apiClient = new HttpClient();
            apiClient.BaseAddress = new Uri("http://localhost:5001/elsa/api");
            //apiClient.GetAsync("http://localhost:5008/elsa/api/workflow-instances");
            //IWorkflowInstancesApi WorkflowInstancesApi = null;
            //WorkflowInstancesApi.ListAsync(new());
            //var instanceList = await api.ListAsync(new());
            var instanceList =await apiClient.GetFromJsonAsync<PagedListResponse<WorkflowInstanceSummary>>("/workflow-instances");
            var instance =instanceList.Items.FirstOrDefault(i => i.CorrelationId == CorrelationId);
            Log.Information($"instance:{JsonSerializer.Serialize(instance)}");
            return instance;
        }

        public async Task ResumeFromBookmark(ResumeBookmarkRequest request, Type? ActivityType)
        {
            await BookmarkResumer.ResumeAsync(request);
            //await BookmarkResumer.ResumeAsync<CommonCorePlugActivity>(request.BookmarkId);
        }

        public async Task ResumeFromBookmark(BookmarkFilter filter)
        {
            var result = await BookmarkResumer.ResumeAsync(filter);
            if (result.Matched)
            {
                //若确实执行成功说明，说明已经直接恢复书签了，这里直接返回即可
                Log.Information($"Successfully resumed workflow using bookmark {filter.BookmarkId} for activity type {filter.CorrelationId}");
                return;
            }

            // There was no matching bookmark yet. Store the queue item for the system to pick up whenever the bookmark becomes present.
            Log.Information($"No bookmark with ID {filter.BookmarkId} found for CorrelationId {filter.CorrelationId}. Adding the request to the bookmark queue");

            //await BookmarkResumer.ResumeAsync<CommonCorePlugActivity>(request.BookmarkId);
        }
    }


}
