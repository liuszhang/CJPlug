using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace CJ.Plug.WorkflowCoreIntegration.Contracts
{
    public interface IWorkflowCoreService
    {
        WorkflowDefinition LoadJsonToWorkflow(string jsonString);

        Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null, string reference = null) where TData : class, new();
    }
}
