using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.StationManageApiClient;
using Serilog;
using System.Text;

namespace CSharpPlug.Services
{
    /// <summary>
    /// 桥接程序工具调度服务
    /// 通过工具管理系统将桥接程序作为工具调度到图站执行。
    /// 采用与 PythonPlug.SubmitPythonExecute 相同的直接调度模式：
    /// 查找工具 → 获取图站 → 构建 PowerShell 包装命令 → SubmitNewToolExecute 提交执行。
    /// 
    /// 绕过 ToolExecuteService.ExecuteToolAsync，因为其 EvalCommandLine 会把
    /// PowerShell 的 [System.Text.Encoding] 等类型标记误解析为变量占位符。
    /// </summary>
    public class BridgeToolService
    {
        private readonly MainApiClient _mainApiClient;

        private const string BridgeToolName = ".NET Framework 桥接程序";
        private const string BridgeToolVersion = "1.0";

        public BridgeToolService(MainApiClient mainApiClient)
        {
            _mainApiClient = mainApiClient;
        }

        /// <summary>
        /// 通过工具调度系统调用桥接程序执行 C# 代码
        /// </summary>
        /// <returns>执行结果文本；失败时返回以 "BRIDGE_TOOL_FAILED:" 开头的错误信息</returns>
        public async Task<string> ExecuteViaToolSystemAsync(
            string csharpCode,
            List<string> dllPaths,
            Dictionary<string, string>? envVars = null,
            string pdzId = "",
            string plugDefinitionId = "",
            string correlationId = "")
        {
            try
            {
                // 1. 查找桥接程序工具（验证工具已在种子数据中注册）
                var tool = await _mainApiClient.GetToolByDisplayNameAsync(
                    $"{BridgeToolName}({BridgeToolVersion})");
                if (tool == null)
                {
                    Log.Warning($"桥接程序工具未在工具管理系统中注册: {BridgeToolName}({BridgeToolVersion})");
                    return $"BRIDGE_TOOL_FAILED: 工具未注册: {BridgeToolName}({BridgeToolVersion})";
                }

                // 2. 获取可用的图站
                var stationIp = await _mainApiClient.GetStationToUse();
                if (string.IsNullOrEmpty(stationIp))
                {
                    Log.Warning("无可用的图站来执行桥接程序");
                    return "BRIDGE_TOOL_FAILED: 无可用的图站，请检查配置。";
                }
                stationIp = stationIp.TrimEnd('/');
                Log.Information($"目标图站: {stationIp}");

                // 3. 解决工具路径：优先用工具配置表获取图站上的工具路径
                //    如果配置表没有，则使用工具本身的 ToolPath 作为兜底
                var toolPath = tool.ToolPath;
                var toolConfigPath = await _mainApiClient.GetToolPathByFilter(
                    new CJ.Plug.Models.Station.ToolConfigFilter
                    {
                        StationIP = stationIp,
                        ToolName = BridgeToolName,
                        ToolVersion = BridgeToolVersion
                    });
                if (!string.IsNullOrEmpty(toolConfigPath))
                {
                    toolPath = toolConfigPath;
                    Log.Information($"根据工具配置表更新桥接程序路径: {toolPath}");
                }

                // 4. 构建 PowerShell 包装命令
                //    [toolPath] 直接内联到命令中（绕过 EvalCommandLine 的 [...] 解析问题）
                var command = BuildBridgePowerShellCommand(csharpCode, dllPaths, envVars, toolPath);

                // 5. 构建执行请求并提交到图站（与 PythonPlug.SubmitPythonExecute 相同模式）
                var executionRequest = new PlugExecutionRequest
                {
                    ToolName = BridgeToolName,
                    ToolVersion = BridgeToolVersion,
                    RequestCommand = command,
                    ExecuteMode = ExecuteMode.Standalone,
                    ExecuteResultData = new ExecuteResultData
                    {
                        Ids = new ExecuteIdsBundle
                        {
                            PDZId = pdzId,
                            PlugDefinitionId = plugDefinitionId,
                            JobCorrelationId = correlationId
                        }
                    }
                };

                Log.Information($"通过工具调度系统调用桥接程序: {BridgeToolName} v{BridgeToolVersion}");
                var result = await _mainApiClient.SubmitNewToolExecute(stationIp, executionRequest);

                if (result?.ExecuteSubStatus == JobSubStatus.已完成)
                {
                    return result.ResultString ?? "";
                }

                var errorMsg = result?.ResultString ?? "未知错误";
                Log.Warning($"工具调度执行失败: {errorMsg}");
                return $"BRIDGE_TOOL_FAILED: {errorMsg}";
            }
            catch (Exception ex)
            {
                Log.Error($"工具调度执行异常: {ex.Message}");
                return $"BRIDGE_TOOL_FAILED: {ex.Message}";
            }
        }

        /// <summary>
        /// 构建桥接程序的 PowerShell 包装执行命令
        /// 
        /// 流程：
        ///   1. base64 解码 C# 代码到变量 $d
        ///   2. 生成临时 .cs 文件路径
        ///   3. Set-Content 写入文件
        ///   4. 设置环境变量（如有）
        ///   5. 调用桥接程序执行
        ///   6. Remove-Item 清理临时文件
        ///   
        /// 注意：不使用 [ToolPath] 占位符，避免 EvalCommandLine 误解析 PowerShell 类型标记。
        ///   桥接程序路径直接内联，由调用方传入已解析的 toolPath。
        /// </summary>
        private static string BuildBridgePowerShellCommand(
            string csharpCode,
            List<string> dllPaths,
            Dictionary<string, string>? envVars,
            string toolPath)
        {
            var sb = new StringBuilder();

            // Base64 编码 C# 代码
            var base64Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(csharpCode));
            var dllsArg = string.Join(";", dllPaths.Where(File.Exists));
            var escapedDllsArg = EscapeForPowerShell(dllsArg);
            var escapedToolPath = EscapeForPowerShell(toolPath);

            // 步骤 1: 解码 + 写入临时文件
            sb.Append("$d=[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('");
            sb.Append(base64Code);
            sb.Append("'));");
            sb.Append("$f=$env:TEMP+'\\cs_bridge_'+[System.Guid]::NewGuid().ToString('N')+'.cs';");
            sb.Append("Set-Content -Path $f -Value $d -Encoding UTF8;");

            // 步骤 2: 设置环境变量
            if (envVars != null && envVars.Count > 0)
            {
                foreach (var kv in envVars)
                {
                    var safeValue = kv.Value.Replace("'", "''");
                    sb.Append($"$env:{kv.Key}='{safeValue}';");
                }
            }

            // 步骤 3: 调用桥接程序
            sb.Append("$bridge='");
            sb.Append(escapedToolPath);
            sb.Append("';");
            sb.Append("& $bridge --codefile $f --dlls '");
            sb.Append(escapedDllsArg);
            sb.Append("';");

            // 步骤 4: 清理临时文件
            sb.Append("Remove-Item $f -Force;");

            return sb.ToString();
        }

        /// <summary>
        /// 为 PowerShell 单引号字符串转义（将 ' 替换为 ''）
        /// </summary>
        private static string EscapeForPowerShell(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
