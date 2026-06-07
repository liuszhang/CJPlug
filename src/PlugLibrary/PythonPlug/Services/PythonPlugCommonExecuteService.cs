using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using PythonPlug;
using System.Diagnostics;

public class PythonPlugCommonExecuteService : ScriptPlugExecuteService
{
    public PythonPlugCommonExecuteService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    // ====== ScriptPlugExecuteService abstract members ======

    protected override string ScriptVariableName =>
        InitVariableNames.PythonScript.ToString();

    protected override string[]? DataPrepareVariableNames =>
        Enum.GetNames(typeof(InitVariableNames));

    protected override string ResultVariableName =>
        InitVariableNames.ResultString.ToString();

    // ====== BasePlugExecuteService abstract members ======

    public override bool IsThisPlugTypeKey(string? PlugTypeKey)
        => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    // ====== Override: ExecuteScriptAsync (本地 python 进程) ======

    protected override Task<ExecuteResultData?> ExecuteScriptAsync(
        string scriptCode, PlugExecutionRequest request)
    {
        var result = new ExecuteResultData();

        // 写入临时 .py 文件
        var tempDir = Path.Combine(Path.GetTempPath(), "PythonPlug");
        Directory.CreateDirectory(tempDir);
        var scriptFile = Path.Combine(tempDir, $"script_{Guid.NewGuid():N}.py");

        try
        {
            File.WriteAllText(scriptFile, scriptCode, System.Text.Encoding.UTF8);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                }
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(60000); // 60秒超时

            if (process.ExitCode != 0)
            {
                CLog.Error($"Python 执行失败 (ExitCode={process.ExitCode}): {stderr}");
                result.ResultString = $"执行错误: {stderr.Trim()}";
                result.ExecuteStatus = JobStatus.完成;
                result.ExecuteSubStatus = JobSubStatus.出错;
            }
            else
            {
                CLog.Information($"Python 执行成功，输出长度: {stdout.Length}");
                result.ResultString = stdout.Trim();
                result.ExecuteStatus = JobStatus.完成;
                result.ExecuteSubStatus = JobSubStatus.已完成;
            }
        }
        catch (Exception ex)
        {
            CLog.Error($"Python 进程执行异常: {ex.Message}");
            result.ResultString = $"执行异常: {ex.Message}";
            result.ExecuteStatus = JobStatus.完成;
            result.ExecuteSubStatus = JobSubStatus.出错;
        }
        finally
        {
            try { File.Delete(scriptFile); } catch { }
        }

        return Task.FromResult<ExecuteResultData?>(result);
    }

    // ====== Override: PreprocessScriptAsync (参数替换) ======

    protected override async Task<string> PreprocessScriptAsync(
        string scriptCode, PlugExecutionRequest request)
    {
        if (request.ExecuteMode != ExecuteMode.Standalone && PlugDataZone != null)
        {
            var plugDefId = request.ExecuteResultData!.Ids!.PlugDefinitionId;

            // 回退查找根插头定义
            Plug? rootPlug = null;
            try
            {
                var plugTypeKey = PlugDataZone.PlugDatas?
                    .FirstOrDefault(p => p.PlugDefinitionId == plugDefId)?.PlugTypeKey;
                if (!string.IsNullOrEmpty(plugTypeKey) && MainApiClient != null)
                    rootPlug = await MainApiClient.GetRootPlugByTypeNameAsync(plugTypeKey);
            }
            catch (Exception ex)
            {
                CLog.Warning($"获取根插头定义失败: {ex.Message}");
            }

            return SubstituteParameters(scriptCode, paramName =>
            {
                // 优先级1: PDZ.GetVariableValue
                var val = PlugDataZone.GetVariableValue(plugDefId, paramName);
                if (!string.IsNullOrEmpty(val)) return val;

                // 优先级2: PDZ 变量 DefaultValue
                var pdzVar = PlugDataZone.PlugVariableDatas?
                    .FirstOrDefault(v => v.PlugDefinitionId == plugDefId
                        && string.Equals(v.Name, paramName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(pdzVar?.DefaultValue)) return pdzVar.DefaultValue;

                // 优先级3: 根插头定义变量
                var plugVar = rootPlug?.PlugVariables?
                    .FirstOrDefault(v => string.Equals(v.Name, paramName, StringComparison.OrdinalIgnoreCase));
                if (plugVar != null)
                {
                    if (!string.IsNullOrEmpty(plugVar.Value)) return plugVar.Value;
                    if (!string.IsNullOrEmpty(plugVar.DefaultValue)) return plugVar.DefaultValue;
                }

                return null;
            });
        }
        else
        {
            return SubstituteParameters(scriptCode, request.InputVariables);
        }
    }
}
