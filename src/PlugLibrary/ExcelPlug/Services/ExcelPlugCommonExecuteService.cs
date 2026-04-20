using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace ExcelPlug.Services
{
    public class ExcelPlugCommonExecuteService : IPlugCommonExecute
    {
        private MainApiClient MainApiClient { get; }

        public ExcelPlugCommonExecuteService(MainApiClient mainApiClient)
        {
            MainApiClient = mainApiClient;
        }

        public bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);
        public async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            Plug plugToExecute = context.plugToExecute;
            
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"execute excel plug: {plugToExecute.Name}");

            return await ((IPlugCommonExecute)this).ExecuteResultReport(
                        MainApiClient,
                        plugToExecute,
                        plugExecutionRequest,
                        new Dictionary<string, string>(),
                        JobStatus.完成,
                        JobSubStatus.已完成);
        }


    }
}

