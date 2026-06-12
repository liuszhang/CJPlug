using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugBaseCore.Services;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// 脚本类插头回退处理器。
/// 继承 <see cref="ScriptPlugExecuteService"/>，复用 DataPrepare、PreprocessScript、WriteResult 等通用逻辑。
/// 当 PlugTypeKey 无法匹配到具体的脚本类执行服务时，通过 Category="脚本类" 作为回退，
/// 自动从 PDZ 变量名检测脚本类型（PythonScript / JavaScriptCode / CSharpCode / JavaCode）并执行。
/// </summary>
public class ScriptPlugCategoryFallbackHandler : ScriptPlugExecuteService, IPlugCategoryFallbackHandler
{
    public string Category => "脚本类";

    /// <summary>不匹配任何具体 PlugTypeKey，仅通过 Category 匹配。</summary>
    public override bool IsThisPlugTypeKey(string? PlugTypeKey) => false;

    /// <summary>哨兵值，不会用于实际查找；<see cref="GetScriptCodeAsync"/> 会动态检测。</summary>
    protected override string ScriptVariableName => "__ScriptPlugFallback__";

    /// <summary>DataPrepare 阶段读取 Script 和 ScriptType 变量。</summary>
    protected override string[]? DataPrepareVariableNames =>
        ["Script", "ScriptType"];

    /// <summary>结果写回 PDZ 使用的变量名。</summary>
    protected override string ResultVariableName => "ResultString";

    /// <summary>当前检测到的脚本类型，在 GetScriptCodeAsync 中设置。</summary>
    private ScriptType? _detectedType;

    // ScriptType 字符串 → ScriptType 枚举映射
    private static readonly Dictionary<string, ScriptType> ScriptTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Python"] = ScriptType.Python,
        ["JavaScript"] = ScriptType.JavaScript,
        ["CSharp"] = ScriptType.CSharp,
        ["Java"] = ScriptType.Java,
    };

    // PlugTypeKey → ScriptType 回退映射（当 PDZ 中无 ScriptType 时使用）
    private static readonly Dictionary<string, ScriptType> PlugTypeKeyToScriptType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PythonPlug"] = ScriptType.Python,
        ["JavaPlug"] = ScriptType.Java,
        ["CSharpPlug"] = ScriptType.CSharp,
        ["JavaScript"] = ScriptType.JavaScript,
    };

    public ScriptPlugCategoryFallbackHandler(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    // ====== PlugCommonExecute：提取 PlugVariables 传递至下游 ======

    /// <inheritdoc/>
    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        var plugExecutionRequest = context.plugExecutionRequest;
        var erd = plugExecutionRequest?.ExecuteResultData ?? new ExecuteResultData();
        var plugVariables = context.plugToExecute?.PlugVariables;

        // ---- Step 1: DataPrepare ----
        if (DataPrepareVariableNames is { Length: > 0 })
        {
            if (!await DataPrepare(plugExecutionRequest, DataPrepareVariableNames, plugVariables))
                return await ReportErrorResult(erd);
        }

        CLog.Information($"开始执行 {GetType().Name} 脚本插头", PlugDataZone?.PDZId);

        // ---- Step 2: GetScriptCode ----
        var scriptCode = await GetScriptCodeAsync(plugExecutionRequest!, plugVariables);
        if (scriptCode == null)
        {
            CLog.Error("获取脚本代码失败");
            erd.ResultString = "获取脚本代码失败";
            return await ReportErrorResult(erd);
        }

        if (string.IsNullOrEmpty(scriptCode))
        {
            CLog.Warning($"{ScriptVariableName} 内容为空，跳过执行");
            erd.ResultString = "";
            return await ReportCompletedResult(erd);
        }

        // ---- Step 3: PreprocessScriptAsync ----
        scriptCode = await PreprocessScriptAsync(scriptCode, plugExecutionRequest);

        CLog.Information($"脚本预处理完成，长度: {scriptCode.Length}");

        // ---- Step 4: ExecuteScriptAsync ----
        var result = await ExecuteScriptAsync(scriptCode, plugExecutionRequest);

        // ---- Step 5: WriteResultToPDZ ----
        await WriteResultToPDZAsync(plugExecutionRequest, result);

        // 修复：确保 result 包含正确的 Ids（PDZId/PlugDefinitionId/JobCorrelationId），
        // 否则 ExecuteResultReport 无法定位作业，前端状态始终显示"进行中"。
        // RunProcess/RunJava 创建的 ExecuteResultData 未设置 Ids。
        if (result != null)
        {
            result.Ids = erd.Ids;
        }

        // ---- Step 6: ReportCompletedResult ----
        return await ReportCompletedResult(result ?? erd);
    }

    // ====== GetScriptCodeAsync：统一使用 Script 变量 ======

    /// <summary>
    /// 从 PDZ 读取 Script 变量获取脚本代码，同时读取 ScriptType 变量确定脚本类型。
    /// </summary>
    protected override async Task<string?> GetScriptCodeAsync(PlugExecutionRequest request, List<PlugVariable>? plugVariables=null)
    {
        _detectedType = null;

        if (request.ExecuteMode != ExecuteMode.Standalone)
        {
            // Load PlugDataZone if null
            if (PlugDataZone == null)
            {
                PDZApiClient ??= _serviceProvider.GetRequiredService<IPDZApiClient>();
                PlugDataZone = await PDZApiClient.GetPDZByPDZIdAsync(
                    request.ExecuteResultData!.Ids!.PDZId);
                if (PlugDataZone == null)
                {
                    CLog.Error($"未找到数据空间：{request.ExecuteResultData.Ids!.PDZId}");
                    return null;
                }
            }

            var plugDefId = request.ExecuteResultData?.Ids?.PlugDefinitionId;
            var scriptCode = VariableResolver.ResolveFromPDZ("Script", PlugDataZone, plugDefId!, plugVariables);
            if (string.IsNullOrEmpty(scriptCode))
                return null;

            // 读取 ScriptType 确定语言
            var scriptTypeStr = VariableResolver.ResolveFromPDZ("ScriptType", PlugDataZone, plugDefId!, plugVariables);
            if (!string.IsNullOrEmpty(scriptTypeStr) && ScriptTypeMap.TryGetValue(scriptTypeStr, out var mapped))
            {
                _detectedType = mapped;
            }

            // 回退：若 PDZ 中无 ScriptType，通过 request.PlugTypeKey 推断
            if (_detectedType == null && !string.IsNullOrEmpty(request.PlugTypeKey)
                && PlugTypeKeyToScriptType.TryGetValue(request.PlugTypeKey, out var inferred))
            {
                _detectedType = inferred;
                CLog.Warning($"通过 PlugTypeKey 回退推断脚本类型: {request.PlugTypeKey} → {inferred}");
            }

            return scriptCode;
        }
        else
        {
            var vars = request.InputVariables;
            if (vars != null)
            {
                var scriptCode = vars.FirstOrDefault(v => v.Name == "Script")?.Value;
                if (string.IsNullOrEmpty(scriptCode))
                    return null;

                var scriptTypeStr = vars.FirstOrDefault(v => v.Name == "ScriptType")?.Value;
                if (!string.IsNullOrEmpty(scriptTypeStr) && ScriptTypeMap.TryGetValue(scriptTypeStr, out var mapped))
                {
                    _detectedType = mapped;
                }

                // 回退：若 InputVariables 中无 ScriptType，通过 request.PlugTypeKey 推断
                if (_detectedType == null && !string.IsNullOrEmpty(request.PlugTypeKey)
                    && PlugTypeKeyToScriptType.TryGetValue(request.PlugTypeKey, out var standInferred))
                {
                    _detectedType = standInferred;
                    CLog.Warning($"[Standalone] 通过 PlugTypeKey 回退推断脚本类型: {request.PlugTypeKey} → {standInferred}");
                }

                return scriptCode;
            }
        }

        return null;
    }

    // ====== PreprocessScriptAsync：参数通配符替换 ======

    /// <summary>
    /// 对脚本代码执行 ${参数名} 通配符替换。复用 <see cref="ScriptPlugExecuteService.SubstituteParameters"/>。
    /// </summary>
    protected override Task<string> PreprocessScriptAsync(string scriptCode, PlugExecutionRequest request)
    {
        return Task.FromResult(SubstituteParameters(scriptCode, request.InputVariables));
    }

    // ====== ExecuteScriptAsync：按检测到的类型分发执行 ======

    /// <inheritdoc/>
    protected override Task<ExecuteResultData?> ExecuteScriptAsync(
        string scriptCode, PlugExecutionRequest request)
    {
        if (_detectedType == null)
        {
            CLog.Error("[脚本类回退] 未检测到脚本类型，无法执行");
            return Task.FromResult<ExecuteResultData?>(new ExecuteResultData
            {
                ResultString = "脚本类回退执行失败：无法检测脚本类型，" +
                    "请确认插头已正确设置 PlugTypeKey（PythonPlug/JavaScriptPlug/CSharpPlug/JavaPlug）",
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错
            });
        }

        var userName = PlugDataZone?.UserName ?? "Standalone";
        var pdzId = request.PDZId ?? "unknown";
        var plugDefId = request.PlugDefinitionId ?? "unknown";
        var workDir = Path.GetFullPath(Path.Combine(GlobalData.PDZsRootPath, userName, pdzId, plugDefId)).TrimEnd();
        Directory.CreateDirectory(workDir);

        var result = _detectedType.Value switch
        {
            ScriptType.Python => RunProcess("python", "script.py", scriptCode, workDir),
            ScriptType.JavaScript => RunProcess("node", "script.js", scriptCode, workDir),
            ScriptType.CSharp => RunProcess("dotnet-script", "script.csx", scriptCode, workDir),
            ScriptType.Java => RunJava(scriptCode, workDir),
            _ => Task.FromResult(new ExecuteResultData
            {
                ResultString = $"不支持的脚本类型: {_detectedType}",
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错
            })
        };
        return result;
    }

    // ====== Process 执行辅助方法 ======

    private static async Task<ExecuteResultData> RunProcess(
        string exe, string fileName, string code, string workDir)
    {
        var scriptFile = Path.Combine(workDir, fileName);
        await File.WriteAllTextAsync(scriptFile, code, System.Text.Encoding.UTF8);

        var workingDir = Directory.Exists(workDir) ? workDir : Path.GetDirectoryName(scriptFile)!;

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = $"\"{scriptFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDir
                }
            };

            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0
                ? new ExecuteResultData
                {
                    ResultString = stdout.Trim(),
                    ExecuteStatus = JobStatus.完成,
                    ExecuteSubStatus = JobSubStatus.已完成
                }
                : new ExecuteResultData
                {
                    ResultString = $"{exe} 执行失败 (退出码 {process.ExitCode}): {stderr.Trim()}",
                    ExecuteStatus = JobStatus.完成,
                    ExecuteSubStatus = JobSubStatus.出错
                };
        }
        catch (Win32Exception ex)
        {
            CLog.Error($"[脚本类回退] Process.Start 失败: {ex.Message}, 工作目录: {workingDir}");
            return new ExecuteResultData
            {
                ResultString = $"启动 {exe} 失败 (Win32 error {ex.NativeErrorCode}): {ex.Message}, 工作目录: {workingDir}",
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错
            };
        }
    }

    private static async Task<ExecuteResultData> RunJava(string code, string workDir)
    {
        var javaFile = Path.Combine(workDir, "Main.java");
        await File.WriteAllTextAsync(javaFile, code, System.Text.Encoding.UTF8);

        // javac 编译
        using (var compile = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "javac",
                Arguments = $"\"{javaFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            }
        })
        {
            compile.Start();
            var compileErr = await compile.StandardError.ReadToEndAsync();
            await compile.WaitForExitAsync();

            if (compile.ExitCode != 0)
                return new ExecuteResultData
                {
                    ResultString = $"Java 编译失败: {compileErr.Trim()}",
                    ExecuteStatus = JobStatus.完成,
                    ExecuteSubStatus = JobSubStatus.出错
                };
        }

        // java 执行
        using var run = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = "-cp . Main",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            }
        };

        run.Start();
        var stdout = await run.StandardOutput.ReadToEndAsync();
        var stderr = await run.StandardError.ReadToEndAsync();
        await run.WaitForExitAsync();

        return run.ExitCode == 0
            ? new ExecuteResultData
            {
                ResultString = stdout.Trim(),
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.已完成
            }
            : new ExecuteResultData
            {
                ResultString = $"Java 执行失败 (退出码 {run.ExitCode}): {stderr.Trim()}",
                ExecuteStatus = JobStatus.完成,
                ExecuteSubStatus = JobSubStatus.出错
            };
    }

    private enum ScriptType { Python, JavaScript, CSharp, Java }
}
