using CJ.Plug.Models.Job;
using CJ.Plug.PlugBaseCore.Models;
using DllLoaderPlug;
using Serilog;

namespace DllLoaderPlug.Services
{
    public class DllLoaderPlugExecuteService(IServiceProvider serviceProvider) : BasePlugExecuteService(serviceProvider)
    {

        public override bool IsThisPlugTypeKey(string? PlugTypeKey) => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);


        public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
        {
            //Plug plug = context.plugToExecute;
            
            PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

            Log.Information($"execute csharp plug");
            var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
            if (!await DataPrepare(plugExecutionRequest, Enum.GetNames(typeof(InitVariableNames)))) { return await ReportErrorResult(erd); }

            var pdz = await MainApiClient.GetPDZByPDZIdAsync(erd.Ids.PDZId);
            if (pdz == null)
            {
                erd.ExecuteStatus = JobStatus.完成;
                erd.ExecuteSubStatus = JobSubStatus.出错;
                erd.Outcome = [InitOutcomes.结束.ToString()];
                return await ExecuteResultReport(erd);
            }

            var code = "int a = 10; int b = 20; return a + b;";
            var references = new[]
            {
                "System.Runtime.dll",          // 基础库
                //"path/to/your/sdk.dll"         // 自定义 SDK
            };

            //var result = await CodeRunner.RunCSharpCodeAsync(code, references);
            //Console.WriteLine(result); // 输出: 30
            //Log.Information(result); // 输出: 30


            erd.ExecuteStatus = JobStatus.完成;
            erd.ExecuteSubStatus = JobSubStatus.已完成;
            erd.Outcome = [InitOutcomes.结束.ToString()];
            return await ExecuteResultReport(erd);
        }

        


    }
}

