using System.Text.Json;
using System.Text.Json.Nodes;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.ProcessManageApi.Services;

/// <summary>
/// 工作流翻译器 — 将 AI 生成的 WorkflowGenerationResult 翻译为可保存的 Process 实体 + PDZ 变量。
/// 生成的 Process 可以直接在 ProcessEdit 中打开和继续编辑。
/// </summary>
public class WorkflowTranslator
{
    private readonly CapabilityRegistry _registry;
    private readonly ILogger<WorkflowTranslator> _logger;

    public WorkflowTranslator(
        CapabilityRegistry registry,
        ILogger<WorkflowTranslator> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// 将 AI 生成的工作流翻译为 Process 实体和 PDZ 入口变量。
    /// 返回的 Process 尚未保存到数据库，调用方需自行 SaveChanges。
    /// </summary>
    /// <param name="result">AI 生成的工作流定义</param>
    /// <param name="userName">创建者用户名</param>
    /// <returns>(Process, 入口变量列表)</returns>
    public (Process Process, List<PlugVariable> EntryVariables) Translate(
        WorkflowGenerationResult result, string userName)
    {
        var process = CreateProcess(result, userName);
        var entryVariables = CreateEntryVariables(result, process.DefinitionId);
        var activityJson = BuildActivityJson(result);

        // 设置流程属性
        process.PlugVariables = entryVariables;
        process.ActivityJsonData = activityJson;
        process.Description = result.Description;
        process.Name = result.Name;

        return (process, entryVariables);
    }

    /// <summary>
    /// 验证 AI 生成的工作流是否能翻译（插件是否都存在）
    /// </summary>
    public List<string> ValidateForTranslation(WorkflowGenerationResult result)
    {
        var errors = new List<string>();

        foreach (var activity in result.Activities)
        {
            var cap = _registry.Get(activity.Plugin);
            if (cap == null)
            {
                errors.Add($"插件 '{activity.Plugin}' 未注册。可用的插件: {string.Join(", ", _registry.GetAll().Select(c => c.Name))}");
                continue;
            }

            // 验证参数引用
            foreach (var (key, value) in activity.Params)
            {
                if (value.StartsWith("$") && !value.StartsWith("$prev."))
                {
                    var varName = value.TrimStart('$');
                    var isInput = result.Inputs.Any(i => i.Name == varName);
                    if (!isInput)
                    {
                        _logger.LogWarning(
                            "Activity '{Plugin}' param '{Key}' references unknown variable: {Value}",
                            activity.Plugin, key, value);
                    }
                }
            }
        }

        return errors;
    }

    #region Private Methods

    private Process CreateProcess(WorkflowGenerationResult result, string userName)
    {
        var definitionId = $"{result.Name}_{Guid.NewGuid():N}"[..Math.Min($"{result.Name}_{Guid.NewGuid():N}".Length, 64)];

        return new Process
        {
            DefinitionId = definitionId,
            Name = result.Name,
            Description = result.Description,
            Creater = userName,
            PlugTypeKey = "Process",
            CreateType = PlugCreateTypeEnum.ProcessToPlug.ToString(),
            Status = "设计",
            WorkPath = Path.Combine(userName, "Design", definitionId),
            Version = 1,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            IsRootPlug = true,
        };
    }

    private List<PlugVariable> CreateEntryVariables(
        WorkflowGenerationResult result, string? processDefinitionId)
    {
        return result.Inputs.Select((input, index) => new PlugVariable
        {
            Name = input.Name,
            DisplayName = input.Description,
            Description = input.Description,
            Type = input.Type,
            IsInput = true,
            IsRequired = input.IsRequired,
            IsInitVariable = true,
            IsBrowsable = true,
            Value = "",
        }).ToList();
    }

    /// <summary>
    /// 构建工作流 Activity JSON（Elsa 格式）。
    /// 当前版本生成简化版 Activity 结构，可在 ProcessEdit 中进一步完善。
    /// </summary>
    private string BuildActivityJson(WorkflowGenerationResult result)
    {
        var root = new JsonObject
        {
            ["type"] = "Workflow",
            ["name"] = result.Name,
            ["version"] = 1,
        };

        var activities = new JsonArray();
        foreach (var (activity, idx) in result.Activities.Select((a, i) => (a, i)))
        {
            var cap = _registry.Get(activity.Plugin);
            var activityNode = new JsonObject
            {
                ["id"] = $"activity_{idx + 1}",
                ["name"] = activity.Plugin,
                ["type"] = "CommonCorePlugActivity",
                ["description"] = activity.Description,
                ["order"] = idx + 1,
            };

            // 插件类型信息
            activityNode["plugTypeKey"] = cap?.PluginTypeKey ?? activity.Plugin;

            // 参数（解析 $引用 为实际值占位符）
            var customProperties = new JsonObject();
            foreach (var (key, value) in activity.Params)
            {
                // 保留 $引用 语法，运行时由引擎解析
                customProperties[key] = value;
            }
            activityNode["customProperties"] = customProperties;

            activities.Add(activityNode);
        }

        root["activities"] = activities;
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    #endregion
}
