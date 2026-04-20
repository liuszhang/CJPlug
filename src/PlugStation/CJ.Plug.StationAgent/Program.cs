using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.StationAgent.Contracts;
using CJ.Plug.StationAgent.Services;
using CJ.Plug.StationAgent.Shared;
using CJ.Plug.StationAgent.ToolAgents;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Write("请带参启动");
            //return;
        }

        var services = new ServiceCollection();
        services.AddStationApiClient();
        var serviceProvider = services.BuildServiceProvider();
        //var ApiService = serviceProvider.GetRequiredService<IClientApiService>();
        var StationApiClient = serviceProvider.GetRequiredService<StationApiClient>();

        

        var ExecuteRequest = new PlugExecutionRequest();
        try
        {
            //await ApiService.SendLog("StationAgent started");
            //await ApiService.SendLog("args[0]:" + args[0]);
            // 解码
            byte[] bytes = Convert.FromBase64String(args[0]);
            string decodedJson = Encoding.UTF8.GetString(bytes);
            ExecuteRequest = JsonSerializer.Deserialize<PlugExecutionRequest>(decodedJson);
            //await ApiService.SendLog("ExecuteRequest: ", ExecuteRequest?.ToolFullPath);
            if (ExecuteRequest == null)
            {
                await StationApiClient.SendLog("ExecuteRequest is null");
                await StationApiClient.SendResult(ExecuteRequest, null, JobSubStatus.出错);
                return;
            }
            //await ApiService.SendLog("ExecuteRequest IDs: " + JsonSerializer.Serialize(ExecuteRequest.ExecuteResultData.Ids));

        }
        catch (Exception ex)
        {
            await StationApiClient.SendLog("Error in Main: " + ex.Message);
            await StationApiClient.SendResult(ExecuteRequest, null, JobSubStatus.出错);
            return;
        }


        

        try
        {            
            (var resultString, var status) = await DefaultCmdExecute.ExecuteCMD(ExecuteRequest, StationApiClient);
            //这里除非是测试状态，否则不要做控制台输出，否则会作为实际输出被捕获到前端界面
            //Console.WriteLine(status+"|"+resultString);
            Console.WriteLine(resultString);
            if (ExecuteRequest.ExecuteMode != ExecuteMode.Standalone)
            {
                await StationApiClient.SendResult(ExecuteRequest, resultString, status);
            }
        }
        catch (Exception ex)
        {
            await StationApiClient.SendLog(ex.Message);
            await StationApiClient.SendResult(ExecuteRequest, null, JobSubStatus.出错);
        }
    }
}