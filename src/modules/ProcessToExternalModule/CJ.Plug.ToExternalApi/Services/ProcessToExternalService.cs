using CJ.Plug.Models.Job;
using CJ.Plug.Models.ProcessToExternal;
using CJ.Plug.Models.Shared;
using System.Text.Json;

namespace CJ.Plug.ApiServer.Services
{
    public class ProcessToExternalService : IProcessToExternalService
    {
        public Task<List<ProcessInput>?> GetInputsOfProcess(string processId)
        {
            return Task.FromResult<List<ProcessInput>?>(new List<ProcessInput>
            {
                new ProcessInput("Name", "string", true, "Default Name"),
                new ProcessInput("Type", "int", true, "Default Type"),
                new ProcessInput("Required", "bool", false, "true"),
                new ProcessInput("Value", "string", false, "Default Value")
            });
        }

        public Task<ExecuteResultData?> GetProcessResultData(string processId)
        {
            return Task.FromResult<ExecuteResultData?>(new ExecuteResultData
            {
                Ids = new ExecuteIdsBundle { JobCorrelationId = processId },
                ExecuteStatus = JobStatus.执行中,
                ExecuteSubStatus = JobSubStatus.提交,
                ExecuteResultMessage = "Process started successfully.",
                ResultString = "Sample result string",
                ResultFileInformations = new List<FileInformation>
                {
                    new FileInformation { FileName = "result.txt", FilePath = "/path/to/result.txt" }
                }
            });
        }

        public Task<ProcessStatus?> GetStatusOfProcess(string processId)
        {
            return Task.FromResult<ProcessStatus?>(new ProcessStatus
            {
                ProcessId = processId,
                StartTime = DateTime.UtcNow.AddMinutes(-10), // 假设流程开始于10分钟前
                UpdateTime = DateTime.UtcNow,
                Status = "Running",
                SubStatus = "In Progress"
            });
        }

        /// <summary>
        /// 对外提供的执行流程方法，执行指定的流程ID，可接受多样的表单数据。
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="form"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ProcessSubmitResult?> StartProcess(string processId)
        {
            return await Task.FromResult<ProcessSubmitResult?>(new ProcessSubmitResult(processId, "Process started successfully."));
        }
    }
}
