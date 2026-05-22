using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;

using CJ.Plug.Models.Plug;
using CJ.Plug.StationApiServer.Contracts;
using CJ.Plug.StationApiService.Contracts;
using CJ.Plug_Aspire.StationApiService.Models;
using CJ.Plug_Aspire.StationApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace CJ.Plug_Aspire.StationApiService.StationApi
{
    public static class StationApi
    {
        public static IEndpointRouteBuilder MapConnectionApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/station");

            api.MapGet("/executeAction/{action}", ExecuteAction);
            //api.MapPost("/executeToolCommand", async (StationExecutionRequest request) =>await ExecuteToolCommand(request));
            api.MapPost("/executeToolCommand", async (IStationExecuteService service, PlugExecutionRequest request) => await service.ExecuteRequestCommand(request));
            api.MapPost("/reportResult", async (IStationExecuteService service, [FromBody] ExecuteResultData result) => await service.ReportExecuteResult(result));
            api.MapPost("/sendLog", async (IStationExecuteService service, LogModel log) => await service.SendLog(log));

            api.MapGet("/DownloadFileByFileId/{fileId}", async (IStationFileService service, string fileId, string fileName) => await service.DownloadFileByFileId(fileId,fileName));
            api.MapPost("/UploadFileToVariable", async (IStationFileService service, [FromBody] PlugVariableData request) => await service.UploadFileToVariable(request));



            // 连接状态接口 - 供 StationSettingUI 查询图站与主服务器的长连接状态
            api.MapGet("/connection-status", (StationHubService hubService) =>
            {
                return TypedResults.Ok(new
                {
                    HubConnected = hubService.IsHubConnected,
                    MainServerUrl = hubService.MainServerUrl,
                    LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                });
            });

            // 图站任务列表
            api.MapGet("/tasks", GetTasks);

            // 图站任务详情
            api.MapGet("/tasks/{id}", GetTaskById);

            // 手动停止任务
            api.MapPost("/tasks/{id}/stop", async (IStationExecuteService service, int id) =>
            {
                await service.StopTask(id);
                return TypedResults.Ok(new { message = "已发送停止指令" });
            });

            api.MapGet("/test", () => TypedResults.Ok("pong!"));


            return app;
        }


        public static Task<string> ExecuteAction(string action)
        {
            Log.Information("Station执行消息-" + action);
            string tmp = "";
            // 创建 ProcessStartInfo 对象，设置 FileName 为 notepad.exe
            var startInfo = new ProcessStartInfo("notepad.exe")
            {
                UseShellExecute = true, // 允许用户交互
                CreateNoWindow = false // 创建窗口，以便用户可以看到并操作应用程序
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;

                // 添加 Exited 事件处理器来处理进程退出时的操作
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    Log.Information("Notepad has been closed.");
                    // 在这里可以添加额外的逻辑，例如通知用户或进行其他清理工作
                };

                // 启动进程
                process.Start();

                // 输出进程 ID
                Log.Information($"Notepad started with process id: {process.Id}");
                tmp += $"Notepad started with process id: {process.Id}\n";

                // 这里可以做其他事情，或者等待用户指令

                // 等待一段时间后检查进程是否已经退出
                for (int i = 0; i < 50; i++)
                {
                    if (process.HasExited)
                    {
                        Log.Information("Process has already exited.");
                        break;
                    }
                    Log.Information("Checking if Notepad is still running...");
                    System.Threading.Thread.Sleep(3000); // 每隔3秒检查一次
                }

                // 如果程序没有退出，我们可以选择等待它退出
                if (!process.HasExited)
                {
                    Log.Information("Waiting for Notepad to exit...");
                    process.WaitForExit(); // 阻塞直到进程退出
                }

                Log.Information("Notepad has finished execution.");
                tmp += "Notepad has finished execution.\n";
            }

            return Task.FromResult("Station返回消息：\n" + tmp);
        }

        private static IResult GetTasks(StationTaskStore taskStore)
        {
            taskStore.FixStaleRunningTasks();
            var tasks = taskStore.GetAll(200);
            return TypedResults.Ok(tasks);
        }

        private static IResult GetTaskById(StationTaskStore taskStore, int id)
        {
            var task = taskStore.GetById(id);
            return task is not null ? TypedResults.Ok(task) : TypedResults.NotFound();
        }
    }
}
