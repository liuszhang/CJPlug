
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.ProcessToExternal;
using CJ.Plug.Models.Station;

public interface IProcessToExternalService
{
    Task<List<ProcessInput>?> GetInputsOfProcess(string processId);
    Task<ProcessSubmitResult?> StartProcess(string processId);
    Task<ProcessStatus?> GetStatusOfProcess(string processId);
    Task<ExecuteResultData?> GetProcessResultData(string processId);

}