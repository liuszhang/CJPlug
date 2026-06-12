using CJ.Plug.JobManageApiClient;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Station;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.StationAndToolApiClient;
using CJ.Plug.TASApiClient;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;

public class ExecuteApiClient : BaseApiClient, IExecuteApiClient
{
    private readonly IServiceProvider _serviceProvider;
    private ITASApiClient? TasApiClient;
    private IStationAndToolApiClient? StationAndToolApiClient;
    private IPDZApiClient? PDZApiClient;
    private IJobManageApiClient? JobManageApiClient;

    public ExecuteApiClient(HttpClient dispatcherClient, IServiceProvider serviceProvider) : base(dispatcherClient)
    {
        _serviceProvider = serviceProvider;
    }
    [Obsolete]
    public async Task<ExecuteResultData?> ExecutePlugByDefifnitionId(string definitionId, string? CorrelationId = null, CancellationToken cancellationToken = default)
    {
        var executeRequest = new PlugExecutionRequest();
        executeRequest.ExecuteResultData.Ids.PlugDefinitionId = definitionId;
        executeRequest.ExecuteResultData.Ids.JobCorrelationId = CorrelationId;

        var response = await ExecutePlug(executeRequest, cancellationToken);
        //var response = await httpClient.GetAsync($"/api/plug/executePlugByDefinitionId/{definitionId}", cancellationToken);
        //if (!string.IsNullOrEmpty(CorrelationId))
        //{
        //    var job = await GetJobByCorrelationIdAsync(CorrelationId);
        //    var instanceId = job.EngineInstanceId;
        //    Log.Information(instanceId + "-" + CorrelationId + "-" + definitionId);
        //    Log.Information("ResumeElsaProcess:" + instanceId + "-" + CorrelationId + "-" + definitionId);
        //}
        return response;
    }

    [Obsolete("Use ExecutePlug instead.")]
    public async Task ExecutePlugWithRequest(string DefinitionId, PlugExecutionRequest plugExecutionRequest, CancellationToken cancellationToken = default)
    {
        //var job= await GetJobByCorrelationIdAsync(plugExecutionRequest.Ids?.CorrelationId);
        //plugExecutionRequest.Ids.ProcessJobInstanceId = job?.InstanceId;
        var response = await httpClient.PostAsJsonAsync($"/api/plug/executePlug/{DefinitionId}", plugExecutionRequest, cancellationToken);
        //Log.Information(response);
        //var result = await response.Content.ReadAsStringAsync();
        //Log.Information(result);
        //return result;
    }

    //-----------------------------------------------------------//



    public async Task<string?> ExecuteToolCommand(PlugExecutionRequest request)
    {
        var result = await httpClient.PostAsJsonAsync("/api/plug/executeToolCommand", request);
        return null;
    }


    public async Task<ExecuteResultData?> ExecutePlugByType(PlugExecutionRequest PlugExecutionRequest, CancellationToken cancellationToken = default)
    {
        string plugType = PlugExecutionRequest.PlugTypeKey;
        TasApiClient= TasApiClient?? _serviceProvider.GetRequiredService<ITASApiClient>();
        var plug = await TasApiClient.GetRootPlugByTypeNameAsync(plugType);
        if (plug == null)
        {
            CLog.Information($"插头类型{plugType}未配置，执行失败。");
            return null;
        }
        CLog.Information($"准备执行插头：{plug.Name}({plug.Id})");
        //PlugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId = plug.DefinitionId;
        var response = await ExecutePlug(PlugExecutionRequest, cancellationToken);
        //Log.Information(response);
        return response;
    }

    public async Task<ExecuteResultData?> ExecutePlug(PlugExecutionRequest plugExecutionRequest, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/plug/executePlug", plugExecutionRequest, cancellationToken);
        //var result = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<ExecuteResultData>(cancellationToken: cancellationToken);
        return result;
    }

    public async Task<ExecuteResultData?> ExecuteOnStation(PlugExecutionRequest stationExectionRequest)
    {
        CLog.Information("start to execute:" + (stationExectionRequest.RequestCommand));
        //Log.Information("start to execute:" + JsonSerializer.Serialize(stationExectionRequest));
        //Console.WriteLine("the execution string is:" + JsonSerializer.Serialize(stationExectionRequest.RequestCommand));

        //此处应动态获取图站IP
        StationAndToolApiClient= StationAndToolApiClient??_serviceProvider.GetService<IStationAndToolApiClient>();
        var stationIp = await StationAndToolApiClient.GetStationToUse();
        if (string.IsNullOrEmpty(stationIp))
        {
            CLog.Error("可使用的图站为空，请检查配置。");
            return new ExecuteResultData
            {
                Ids = stationExectionRequest.ExecuteResultData.Ids,
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                ResultString = "可使用的图站为空，请检查配置。"
            };
        }
        //Log.Information("获取的图站IP：" + stationIp);
        stationIp = stationIp.TrimEnd('/');
        CLog.Information("目标图站：" + stationIp);
        //Log.Information("准备通过图站IP获取特定工具路径");

        //如果工具名称不为空，则获取对应的工具路径
        if (!string.IsNullOrEmpty(stationExectionRequest.ToolName))
        {
            //获取图站IP后再获取对应的工具路径
            //var toolPathFromConfigTable=await GetToolPathOnIp(stationIp, stationExectionRequest.ToolName,stationExectionRequest.ToolVersion);
            var filter = new ToolConfigFilter()
            {
                StationIP = stationIp,
                ToolName = stationExectionRequest.ToolName,
                ToolVersion = stationExectionRequest.ToolVersion
            };
            var toolPathByConfig = await StationAndToolApiClient.GetToolPathByFilter(filter);
            if (!string.IsNullOrEmpty(toolPathByConfig))
            {
                CLog.Information("根据配置表更新工具路径：" + toolPathByConfig);
                //stationExectionRequest.ToolFullPath = toolPathByConfig;
                stationExectionRequest.RequestCommand = stationExectionRequest.RequestCommand?.Replace("[ToolPath]", toolPathByConfig);
            }
        }


        // 确保 stationIp 是完整的 URI 格式
        if (stationIp.StartsWith("://"))
            stationIp = "http" + stationIp;
        else if (!stationIp.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                 !stationIp.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            stationIp = "http://" + stationIp;
        StationApiClient stationApiClient = new StationApiClient(new HttpClient() { BaseAddress = new Uri(stationIp) });
        try
        {
            if (stationExectionRequest.ExecuteMode == ExecuteMode.Standalone)
            {
                //Log.Information("---插头独立执行模式---");
                CLog.Information("need wait for result");
                //Console.WriteLine("need wait for result");
                var response = await stationApiClient.StationExecutionWaitResultAsync(stationExectionRequest);
                return response;
            }
            else
            {
                //Log.Information("no wait for result");
                //Console.WriteLine("no wait for result");
                return await stationApiClient.StationToolExecutionAsync(stationExectionRequest);
            }

        }
        catch (Exception ex)
        {
            CLog.Error("error:" + ex.Message);
            return new ExecuteResultData
            {
                Ids = stationExectionRequest.ExecuteResultData.Ids,
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错,
                ResultString = ex.Message
            };
        }
    }

    


    //提交执行并创建作业和作业PDZ
    public async Task<(string?, string?)> SubmitExecute(PlugData PlugData, string UserName, PlugDataZone PlugDataZone, PDZTypeEnum PDZType)
    {
        //创建作业
        var job = new ProcessJob
        {
            JobDefinitionId = RandomLongIdentityGenerator.GenerateId(),
            ProcessDefinitionId = PlugData.PlugDefinitionId,
            Name = PlugData.Name,
            CreatedAt = DateTimeOffset.UtcNow.ToLocalTime(),
            UpdatedAt = DateTimeOffset.UtcNow.ToLocalTime()
        };

        //获取PDZ
        //PlugDataZone=await MainDbContext.PlugDataZones.FindAsync(PlugDataZone.PDZId);

        //创建执行作业PDZ
        var JobPDZ = PlugDataZone.CopyPDZ(UserName, PDZType.ToString(), job.JobDefinitionId);
        PDZApiClient= PDZApiClient??_serviceProvider.GetService<IPDZApiClient>();
        await PDZApiClient.CreateOrUpdatePDZ(JobPDZ);
        job.JobCorrelationId = JobPDZ.PDZId;
        JobManageApiClient= JobManageApiClient??_serviceProvider?.GetService<IJobManageApiClient>();
        await JobManageApiClient.CreateJobAsync(job);
        return (JobPDZ.PDZId, job.JobDefinitionId);
    }


    public async Task ExecuteResultReport(ExecuteResultData executeReport)
    {
        var response = await httpClient.PostAsJsonAsync("/api/plug/ReportExecuteResult", executeReport);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            CLog.Error(ex.Message);
        }

    }

}
