using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.StationApiServer.Contracts;
using CJ.Plug_Aspire.StationApiService.Models;
using CJ.Plug_Aspire.StationApiService.Services.ToolActionExecute;
using MediatR;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace CJ.Plug_Aspire.StationApiService.Services
{
    public class DefaultStationExecuteService : IStationExecuteService
    {
        private MainApiClient MainApiClient { get; set; }
        private readonly StationTaskStore _taskStore;

        public DefaultStationExecuteService(MainApiClient mainApiClient, StationTaskStore taskStore)
        {
            MainApiClient = mainApiClient;
            _taskStore = taskStore;
        }

        public async Task<ExecuteResultData?> ExecuteRequestCommand(PlugExecutionRequest stationExecutionRequest)
        {
            // 保存任务到本地 SQLite
            var taskId = SaveRequestAsTask(stationExecutionRequest);

            // 更新状态为 running
            _taskStore.UpdateStatus(taskId, "running", null, null);

            if (stationExecutionRequest.ExecuteMode == ExecuteMode.Standalone)
            {
                var result = await ExecuteActions.InvokeStationAgent(stationExecutionRequest);

                // 更新任务结果
                _taskStore.UpdateStatus(taskId, "completed",
                    result?.ExecuteSubStatus.ToString(),
                    JsonSerializer.Serialize(result));

                return result;
            }

            // 异步执行 — 结果通过 ReportExecuteResult 回调更新
            var pid = await ExecuteActions.InvokeStationAgentAsync(stationExecutionRequest);
            if (pid > 0)
            {
                _taskStore.UpdateProcessId(taskId, pid);
            }
            return null;
        }

        /// <summary>
        /// 手动停止任务 — 终止进程、更新状态、向主服务汇报
        /// </summary>
        public async Task StopTask(int taskId)
        {
            var task = _taskStore.GetById(taskId);
            var pid = task?.ProcessId;

            // 1. 终止进程
            if (pid != null)
            {
                try
                {
                    var process = Process.GetProcessById(pid.Value);
                    process.Kill(entireProcessTree: true);
                    Log.Information($"已终止进程 PID={pid}");
                }
                catch (ArgumentException)
                {
                    Log.Information($"进程 PID={pid} 已不存在");
                }
                catch (Exception ex)
                {
                    Log.Error($"终止进程 PID={pid} 失败: {ex.Message}");
                }
            }

            // 2. 更新本地任务状态
            _taskStore.UpdateStatus(taskId, "failed", "手动停止", null);

            // 3. 向主服务汇报
            if (task?.CorrelationId != null)
            {
                try
                {
                    var toolJob = await MainApiClient.GetToolJobByCorrelationIdAsync(task.CorrelationId);
                    if (toolJob != null && toolJob.JobCategory == JobCategoryEnum.ToolJob.ToString())
                    {
                        var executeResultData = new ExecuteResultData
                        {
                            Ids = new ExecuteIdsBundle
                            {
                                ToolJobCorrelationId = task.CorrelationId,
                                JobCorrelationId = task.JobCorrelationId,
                                PlugDefinitionId = task.PlugDefinitionId,
                                PDZId = task.PDZId,
                            },
                            ExecuteStatus = JobStatus.完成,
                            ExecuteSubStatus = JobSubStatus.已取消,
                            ResultString = "任务被手动停止",
                        };

                        toolJob.JobStatus = executeResultData.ExecuteStatus.ToString();
                        toolJob.JobSubStatus = executeResultData.ExecuteSubStatus.ToString();
                        toolJob.ExecuteResultData = JsonSerializer.Serialize(executeResultData);

                        await MainApiClient.UpdateToolJobAsync(toolJob);

                        // 通知主服务流程引擎：任务已取消
                        await MainApiClient.ExecuteResultReport(executeResultData);

                        Log.Information($"已向主服务汇报任务 {taskId} 停止状态");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"向主服务汇报停止状态失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 供 StationAgent 调用的执行结果上报方法
        /// </summary>
        public async Task ReportExecuteResult(ExecuteResultData executeResultData)
        {
            var toolJob = await MainApiClient.GetToolJobByCorrelationIdAsync(executeResultData.Ids.ToolJobCorrelationId);
            if (toolJob == null)
            {
                CLog.Error($"未找到对应的作业，CorrelationId: {executeResultData.Ids.ToolJobCorrelationId}");
                return;
            }

            if (toolJob.JobCategory != JobCategoryEnum.ToolJob.ToString())
            {
                CLog.Error($"作业类型不匹配，预期 ToolJob，但实际为{toolJob.JobCategory}");
                return;
            }

            toolJob.JobStatus = executeResultData.ExecuteStatus.ToString();
            toolJob.JobSubStatus = executeResultData.ExecuteSubStatus.ToString();
            toolJob.ExecuteResultData = JsonSerializer.Serialize(executeResultData);

            var result = await MainApiClient.UpdateToolJobAsync(toolJob);

            // 更新本地任务状态
            var status = executeResultData.ExecuteStatus.ToString() == "Success" ? "completed" : "failed";
            _taskStore.UpdateStatusByCorrelationId(
                executeResultData.Ids.ToolJobCorrelationId!,
                status,
                executeResultData.ExecuteSubStatus.ToString(),
                JsonSerializer.Serialize(executeResultData));

            StatusReporter.JobStatusUpdated(executeResultData.Ids.ToolJobCorrelationId);

            if (!string.IsNullOrEmpty(executeResultData.Ids.PlugDefinitionId))
            {
                await MainApiClient.ExecuteResultReport(executeResultData);
            }
        }

        public async Task SendLog(LogModel log)
        {
            Log.Information(log.Description);
        }

        /// <summary>
        /// 将执行请求保存为本地任务
        /// </summary>
        private int SaveRequestAsTask(PlugExecutionRequest request)
        {
            var task = new StationTask
            {
                CorrelationId = request.ExecuteResultData?.Ids?.ToolJobCorrelationId,
                JobCorrelationId = request.ExecuteResultData?.Ids?.JobCorrelationId,
                PlugDefinitionId = request.ExecuteResultData?.Ids?.PlugDefinitionId,
                PDZId = request.ExecuteResultData?.Ids?.PDZId,
                PlugTypeKey = request.PlugTypeKey,
                ToolName = request.ToolName,
                Command = request.RequestCommand,
                ExecuteMode = request.ExecuteMode.ToString(),
                Status = "pending",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            };

            return _taskStore.Insert(task);
        }
    }
}
