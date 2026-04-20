using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Shared;
using System.Net.Http.Json;

namespace CJ.Plug.ProcessManageApiClient
{
    public class ProcessManageApiClient:BaseApiClient,IProcessManageApiClient
    {

        public ProcessManageApiClient(HttpClient dispatcherClient):base(dispatcherClient) 
        {
        }


        public async Task<Process[]> GetWorkflowsAsync(int maxItems = 20, CancellationToken cancellationToken = default)
        {
            List<Process>? Workflows = null;

            await foreach (var workflow in httpClient.GetFromJsonAsAsyncEnumerable<Process>("/api/process/getWorkflows", cancellationToken))
            {
                if (Workflows?.Count >= maxItems)
                {
                    //break;
                }
                if (workflow is not null)
                {
                    Workflows ??= [];
                    Workflows.Add(workflow);
                }
            }

            return Workflows?.ToArray() ?? [];
        }

        public async Task<Process> CreateNewWorkflow(Process newWorkflow, CancellationToken cancellationToken = default)
        {
            Process? Workflow = newWorkflow;
            //Console.WriteLine(">>>>>>>>>>>>>>from web:"+JsonSerializer.Serialize(Workflow));
            var result = await httpClient.PostAsJsonAsync<Process>("/api/process/createWorkflow", Workflow, cancellationToken);


            return Workflow;
        }



        public async Task<bool> UpdateProcessAsync(int? workflowId, Process workflow, CancellationToken cancellationToken = default)
        {
            //Console.WriteLine(">>>>>>>>>>>>>>from web variables is:"+JsonSerializer.Serialize(workflow.ProcessVariables));
            var result = await httpClient.PutAsJsonAsync($"/api/process/updateWorkflow/{workflowId}", workflow, cancellationToken);
            //return true;
            // 检查响应状态码
            if (result.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                // 处理错误情况
                var errorMessage = await result.Content.ReadAsStringAsync();
                Console.WriteLine($"Error updating workflow: {errorMessage}");
                return false;
            }
        }

    }
}
