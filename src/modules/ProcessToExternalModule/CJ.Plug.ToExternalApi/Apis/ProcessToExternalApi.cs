public static class ProcessExecuteApi
{
    public static IEndpointRouteBuilder MapProcessToExternalApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/cjprocess").WithTags("流程执行(对外)");

        //查询流程输入参数
        api.MapGet("/getInputs/{ProcessId}", async (IProcessToExternalService service, string ProcessId) => await service.GetInputsOfProcess(ProcessId));

        //对外提供的流程执行接口，接收表单数据
        api.MapPost("/start/{ProcessId}", async (IProcessToExternalService service, string ProcessId) => await service.StartProcess(ProcessId));
        //查询流程执行状态
        api.MapGet("/getProcessStatus/{ProcessId}", async (IProcessToExternalService service, string ProcessId) => await service.GetStatusOfProcess(ProcessId));
        //获取流程执行结果
        api.MapGet("/getProcessResultData/{ProcessId}", async (IProcessToExternalService service, string ProcessId) => await service.GetProcessResultData(ProcessId));



        return app;
    }

}

