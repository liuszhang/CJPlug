using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Serilog;

namespace NastranPlug.Services
{
    public class NastranPlugExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
    {

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            //Plug plug = context.plugToExecute;

            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"execute nastran plug");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
            return await ReportCompletedResult(erd);
        }




    }
}
