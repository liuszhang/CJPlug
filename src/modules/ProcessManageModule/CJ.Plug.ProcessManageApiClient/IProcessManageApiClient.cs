using System.Net.Http.Json;
using System.Text.Json;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Shared;

namespace CJ.Plug.ProcessManageApiClient
{
    public interface IProcessManageApiClient
    {
        Task<Process> CreateNewWorkflow(Process newWorkflow, CancellationToken cancellationToken = default);
        Task<Process[]> GetWorkflowsAsync(int maxItems = 20, string? userName = null, CancellationToken cancellationToken = default);
        Task<bool> UpdateProcessAsync(int? workflowId, Process workflow, CancellationToken cancellationToken = default);

        /// <summary>
        /// AI 生成工作流定义（不保存）
        /// </summary>
        Task<JsonElement> AiGenerateWorkflowAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// AI 生成的工作流保存到数据库
        /// </summary>
        Task<JsonElement> AiSaveWorkflowAsync(JsonElement result, CancellationToken cancellationToken = default);
    }
}
