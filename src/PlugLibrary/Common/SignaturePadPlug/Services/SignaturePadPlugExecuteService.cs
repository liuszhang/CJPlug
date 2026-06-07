using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace SignaturePadPlug.Services
{
    public class SignaturePadPlugExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
    {

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            //Plug plug = context.plugToExecute;
            
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"execute test standalone plug");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
            return await ReportCompletedResult(erd);
        }




    }
}

