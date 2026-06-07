using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.PlugBaseCore.Services;
using JavaPlug;
using System.Diagnostics;

public class JavaPlugCommonExecuteService : ScriptPlugExecuteService
{
    public JavaPlugCommonExecuteService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    // ====== ScriptPlugExecuteService abstract members ======

    protected override string ScriptVariableName =>
        InitVariableNames.JavaCode.ToString();

    protected override string[]? DataPrepareVariableNames =>
        Enum.GetNames(typeof(InitVariableNames));

    // ====== BasePlugExecuteService abstract members ======

    public override bool IsThisPlugTypeKey(string? PlugTypeKey)
        => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    // ====== Override: ExecuteScriptAsync (本地 javac + java 进程) ======

    protected override Task<ExecuteResultData?> ExecuteScriptAsync(
        string scriptCode, PlugExecutionRequest request)
    {
        var result = new ExecuteResultData();

        // 写入临时 .java 文件
        var tempDir = Path.Combine(Path.GetTempPath(), "JavaPlug", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var javaFile = Path.Combine(tempDir, "Main.java");

        try
        {
            File.WriteAllText(javaFile, scriptCode, System.Text.Encoding.UTF8);

            // Step 1: javac 编译
            using (var compileProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "javac",
                    Arguments = $"\"{javaFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                }
            })
            {
                compileProcess.Start();
                var compileStderr = compileProcess.StandardError.ReadToEnd();
                compileProcess.WaitForExit(30000);

                if (compileProcess.ExitCode != 0)
                {
                    CLog.Error($"Java 编译失败: {compileStderr}");
                    result.ResultString = $"编译错误: {compileStderr.Trim()}";
                    result.ExecuteStatus = JobStatus.完成;
                    result.ExecuteSubStatus = JobSubStatus.出错;
                    return Task.FromResult<ExecuteResultData?>(result);
                }
            }

            // Step 2: java 执行
            using (var runProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-cp . Main",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                }
            })
            {
                runProcess.Start();
                var stdout = runProcess.StandardOutput.ReadToEnd();
                var stderr = runProcess.StandardError.ReadToEnd();
                runProcess.WaitForExit(30000);

                if (runProcess.ExitCode != 0)
                {
                    CLog.Error($"Java 执行失败 (ExitCode={runProcess.ExitCode}): {stderr}");
                    result.ResultString = $"执行错误: {stderr.Trim()}";
                    result.ExecuteStatus = JobStatus.完成;
                    result.ExecuteSubStatus = JobSubStatus.出错;
                }
                else
                {
                    CLog.Information($"Java 执行成功，输出长度: {stdout.Length}");
                    result.ResultString = stdout.Trim();
                    result.ExecuteStatus = JobStatus.完成;
                    result.ExecuteSubStatus = JobSubStatus.已完成;
                }
            }
        }
        catch (Exception ex)
        {
            CLog.Error($"Java 进程执行异常: {ex.Message}");
            result.ResultString = $"执行异常: {ex.Message}";
            result.ExecuteStatus = JobStatus.完成;
            result.ExecuteSubStatus = JobSubStatus.出错;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return Task.FromResult<ExecuteResultData?>(result);
    }

    // PreprocessScriptAsync: 使用默认 no-op（Java 无需参数替换）
}
