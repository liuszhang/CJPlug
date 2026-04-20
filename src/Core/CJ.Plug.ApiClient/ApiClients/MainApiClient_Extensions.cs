using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;

public partial class MainApiClient
{
    //public async Task ExecuteResultReport(ExecuteResultData executeReport)
    //{
    //    var response = await httpClient.PostAsJsonAsync("/api/plug/ReportExecuteResult", executeReport);
    //    try
    //    {
    //        response.EnsureSuccessStatusCode();
    //    }
    //    catch (Exception ex)
    //    {
    //        CLog.Error(ex.Message);
    //    }

    //}

    /// <summary>
    /// 为后续分布式服务的分发做预计
    /// </summary>
    /// <returns></returns>
    //private async Task GetApiServer()
    //{
    //    var BaseAddress = await DispatcherClient.GetStringAsync("api/station/GetApiServer");
    //    httpClient.BaseAddress = new Uri(BaseAddress);
    //}

}

