using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.StationAndToolApiClient;
using Microsoft.Extensions.DependencyInjection;
using PythonPlug;
using Serilog;
using System.Text;

public class PythonPlugCommonExecuteService : BasePlugExecuteService
{
    public PythonPlugCommonExecuteService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public override bool IsThisPlugTypeKey(string? PlugTypeKey) 
        => (PlugTypeKey == PlugKeySetting.CommonExecuteKey);

    public override async Task<ExecuteResultData?> PlugCommonExecute(ExecuteServiceContext context)
    {
        Plug plug = context.plugToExecute;
        PlugExecutionRequest? plugExecutionRequest = context.plugExecutionRequest;

        var resultData = plugExecutionRequest.ExecuteResultData;
        var status = plugExecutionRequest.ExecuteResultData?.ExecuteSubStatus;
        CLog.Information($"开始执行Python插头，状态为{status.ToString()}",
            context.plugExecutionRequest.ExecuteResultData.Ids.PDZId);

        if (status == JobSubStatus.提交)
        {
            resultData = await SubmitPythonExecute(plugExecutionRequest);
            return await ExecuteResultReport(resultData);
        }
        else if (status == JobSubStatus.图站执行完成)
        {
            Log.Information("图站执行完成，执行Python插头数据后处理");
            try
            {
                PDZApiClient = PDZApiClient ?? _serviceProvider.GetRequiredService<IPDZApiClient>();
                var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids.PDZId);
                var resultString = plugExecutionRequest.ExecuteResultData.ResultString;
                var execteId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
                var identityId = execteId.Contains("|") ? execteId.Split('|')[1] : null;

                if (identityId == null)
                {
                    PDZ?.SetVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, 
                        InitVariableNames.ResultString.ToString(), resultString);
                }
                else
                {
                    PDZ?.SetActionVariableValue(plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, 
                        identityId, InitVariableNames.ResultString.ToString(), resultString);
                }

                await PDZApiClient.CreateOrUpdatePDZ(PDZ);
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.已完成;
                return await ExecuteResultReport(resultData);
            }
            catch (Exception ex)
            {
                CLog.Error($"{plug.Name}后处理执行失败：{ex.Message}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return await ExecuteResultReport(resultData);
            }
        }
        return await ExecuteResultReport(resultData);
    }

    /// <summary>
    /// 获取PDZ中指定插头的Python脚本数据
    /// </summary>
    private string? GetPythonScript(PlugDataZone plugDataZone, string PlugDefinitionId, string? IdentityId = null)
    {
        if (IdentityId == null)
        {
            return plugDataZone.GetVariableValue(PlugDefinitionId, InitVariableNames.PythonScript.ToString());
        }
        else
        {
            return plugDataZone.ActionVariableDatas?
                .Where(p => p.ActionIdentityId == IdentityId)
                .Where(p => p.Name == InitVariableNames.PythonScript.ToString())
                .FirstOrDefault()?.Value;
        }
    }

    /// <summary>
    /// 将Python脚本包装为PowerShell命令：base64解码 → 写临时.py文件 → python执行 → 清理临时文件
    /// </summary>
    public static string BuildPythonExecuteCommand(string scriptContent)
    {
        if (string.IsNullOrEmpty(scriptContent))
            return "echo ''";

        var base64Script = Convert.ToBase64String(Encoding.UTF8.GetBytes(scriptContent));
        // 1. 从base64解码出Python脚本到变量 $d
        // 2. 生成临时.py文件路径
        // 3. Set-Content 写入文件
        // 4. python 执行该文件
        // 5. Remove-Item 清理
        return "$d=[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('" + base64Script + "'));"
             + "$f=$env:TEMP + '\\py_' + (Get-Random) + '.py';"
             + "Set-Content -Path $f -Value $d -Encoding UTF8;"
             + "python $f;"
             + "Remove-Item $f -Force";
    }

    public async Task<ExecuteResultData?> SubmitPythonExecute(PlugExecutionRequest? plugExecutionRequest)
    {
        var resultData = plugExecutionRequest.ExecuteResultData;

        var execteId = plugExecutionRequest.ExecuteResultData.Ids?.ExecuteTaskPlugIds?[0];
        var identityId = execteId.Contains("|") ? execteId.Split('|')[1] : null;
        var ExecutionRequest = plugExecutionRequest ?? new PlugExecutionRequest();
        ExecutionRequest.ToolName = "Python";
        ExecutionRequest.ToolVersion = "1.0";

        string? pythonScript = null;
        if (plugExecutionRequest.ExecuteMode != ExecuteMode.Standalone)
        {
            PDZApiClient = PDZApiClient ?? _serviceProvider.GetRequiredService<IPDZApiClient>();
            var PDZ = await PDZApiClient.GetPDZByPDZIdAsync(plugExecutionRequest.ExecuteResultData.Ids?.PDZId);
            if (PDZ == null)
            {
                CLog.Error($"未找到数据空间：{plugExecutionRequest.ExecuteResultData.Ids?.PDZId}");
                resultData.ExecuteStatus = JobStatus.完成;
                resultData.ExecuteSubStatus = JobSubStatus.出错;
                return resultData;
            }
            pythonScript = GetPythonScript(PDZ, plugExecutionRequest.ExecuteResultData.Ids.PlugDefinitionId, identityId);
        }
        else
        {
            pythonScript = plugExecutionRequest.InputVariables
                .FirstOrDefault(v => v.Name == InitVariableNames.PythonScript.ToString())?.Value;
        }

        if (string.IsNullOrEmpty(pythonScript))
        {
            CLog.Warning("Python脚本内容为空，跳过执行");
            resultData.ExecuteStatus = JobStatus.完成;
            resultData.ExecuteSubStatus = JobSubStatus.已完成;
            resultData.ResultString = "";
            return resultData;
        }

        ExecutionRequest.RequestCommand = BuildPythonExecuteCommand(pythonScript);
        CLog.Information($"Python执行命令已生成，脚本长度: {pythonScript.Length}");

        // 直接获取图站并提交执行，不依赖Tool/Station配置表
        var stationAndToolApiClient = _serviceProvider.GetRequiredService<IStationAndToolApiClient>();
        var stationIp = await stationAndToolApiClient.GetStationToUse();
        if (string.IsNullOrEmpty(stationIp))
        {
            CLog.Error("可使用的图站为空，请检查配置。");
            resultData.ExecuteStatus = JobStatus.完成;
            resultData.ExecuteSubStatus = JobSubStatus.出错;
            resultData.ResultString = "可使用的图站为空，请检查配置。";
            return resultData;
        }
        stationIp = stationIp.TrimEnd('/');
        CLog.Information($"目标图站：{stationIp}");

        resultData = await MainApiClient.SubmitNewToolExecute(stationIp, ExecutionRequest);
        return resultData;
    }

}
