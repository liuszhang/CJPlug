using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;

namespace CJ.Plug.StationApiServer.Contracts
{
    public interface IStationExecuteService
    {
        /// <summary>
        /// 执行请求并返回执行结果
        /// </summary>
        /// <param name="stationExecutionRequest"></param>
        /// <returns></returns>
        Task<ExecuteResultData?> ExecuteRequestCommand(PlugExecutionRequest stationExecutionRequest);

        /// <summary>
        /// 汇报执行结果，此方法供StationAgent执行完成后调用
        /// </summary>
        /// <param name="executeResultData"></param>
        /// <returns></returns>
        Task ReportExecuteResult(ExecuteResultData executeResultData);

        /// <summary>
        /// 发送日志到SignalR,此方法供StationAgent执行时调用
        /// </summary>
        Task SendLog(LogModel log);

        /// <summary>
        /// 手动停止任务，终止对应进程
        /// </summary>
        Task StopTask(int taskId);
    }
    
}
