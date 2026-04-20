using CJ.Plug.WorkflowCoreIntegration.Contracts;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services.DefinitionStorage;

namespace CJ.Plug.WorkflowCoreIntegration.Services
{
    public class WorkflowCoreService : IWorkflowCoreService
    {
        private IDefinitionLoader DefinitionLoader;
        private IWorkflowHost WorkflowHost;

        public WorkflowCoreService(
            IDefinitionLoader definitionLoader,
            IWorkflowHost workflowHost)
        {
            DefinitionLoader = definitionLoader;
            WorkflowHost = workflowHost;
        }



        public WorkflowDefinition LoadJsonToWorkflow(string jsonString)
        {   
            
            var workflowId = DefinitionLoader.LoadDefinition(jsonString, Deserializers.Json);
            if (workflowId == null)
            {
                throw new InvalidOperationException("Failed to load workflow definition from JSON.");
            }
            return workflowId;
        }

        public async Task<string> StartWorkflow<TData>(string workflowId, int? version, TData data = null, string reference = null) where TData : class, new()
        {
            return await WorkflowHost.StartWorkflow(workflowId, null, new Dictionary<string, object>());
        }
    }
}
