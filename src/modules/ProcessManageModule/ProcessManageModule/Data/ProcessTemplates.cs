using System.Text.Json;
using System.Text.Json.Nodes;
using CJ.Plug.ProcessManage.Models;

namespace CJ.Plug.ProcessManage.Data;

/// <summary>
/// 内置流程模板 — 每个模板绑定完整 Elsa Flowchart JSON（与 Designer.ReadFlowchartAsync() 输出格式一致）。
/// JSON 中 id/nodeId/definitionId 使用占位符，创建时替换。
/// </summary>
public static class ProcessTemplates
{
    public static List<ProcessTemplate> GetAll() => [PythonDataProcessing, AiIntelligentAnalysis, CadModelProcessing, DocumentAutoGeneration];

    // ── Python 数据处理 ──
    public static ProcessTemplate PythonDataProcessing => new()
    {
        Id = "python_data_processing",
        Name = "Python 数据处理",
        Description = "读取文本文件 → Python 处理 → 输出结果。适用于日志分析、数据清洗、格式转换等。",
        Icon = "Code",
        Category = "数据处理",
        TemplateJson = BuildTemplate("Python 数据处理", [
            ("读取数据",    "TextReaderPlug",  -280.0, -80.0),
            ("Python 处理", "PythonPlug",        120.0, -80.0),
            ("输出结果",    "TextWriterPlug",    520.0, -80.0),
        ]),
    };

    // ── AI 智能分析 ──
    public static ProcessTemplate AiIntelligentAnalysis => new()
    {
        Id = "ai_intelligent_analysis",
        Name = "AI 智能分析",
        Description = "将内容发送给 AI Agent 进行智能分析（审查、总结、翻译、问答等），返回结果。",
        Icon = "Psychology",
        Category = "AI 智能",
        TemplateJson = BuildTemplate("AI 智能分析", [
            ("AI 分析", "AiAgentPlug", 0.0, -80.0),
        ]),
    };

    // ── CAD 模型处理 ──
    public static ProcessTemplate CadModelProcessing => new()
    {
        Id = "cad_model_processing",
        Name = "CAD 模型处理",
        Description = "获取 NX 模型参数 → Python 计算优化 → 回写 NX。适用于参数化设计、优化迭代。",
        Icon = "PrecisionManufacturing",
        Category = "CAD 工程",
        TemplateJson = BuildTemplate("CAD 模型处理", [
            ("获取 NX 参数", "NXGetParameters", -280.0, -80.0),
            ("参数计算",      "PythonPlug",       120.0, -80.0),
            ("回写参数",      "NXSetParameters",  520.0, -80.0),
        ]),
    };

    // ── 文档自动生成 ──
    public static ProcessTemplate DocumentAutoGeneration => new()
    {
        Id = "document_auto_generation",
        Name = "文档自动生成",
        Description = "REST 获取数据 → 填充 Word 模板 → 转 PDF。适用于报告/合同/证书自动生成。",
        Icon = "Article",
        Category = "文档处理",
        TemplateJson = BuildTemplate("文档自动生成", [
            ("获取数据", "RESTPlug",  -280.0, -80.0),
            ("填充 Word", "WordPlug",  120.0, -80.0),
            ("转 PDF",   "WordToPdf", 520.0, -80.0),
        ]),
    };

    // ══════════════════════════════════════════════════════════════════
    //  Helper — 构建与 Designer.ReadFlowchartAsync() 输出一致的 JSON
    // ══════════════════════════════════════════════════════════════════
    private static string BuildTemplate(string name, (string Name, string PlugTypeKey, double X, double Y)[] acts)
    {
        var defId = "{definitionId}";
        var nodeW = 200.0;
        var nodeH = 50.0;

        // 节点
        var activities = new JsonArray();
        var ids = new List<string>();
        for (int i = 0; i < acts.Length; i++)
        {
            var (actName, plugTypeKey, x, y) = acts[i];
            var actId = $"{{activity_{i}_id}}";
            ids.Add(actId);

            activities.Add(new JsonObject
            {
                ["id"]               = actId,
                ["nodeId"]           = $"{defId}:{actId}",
                ["definitionId"]     = actId,
                ["PlugDefinitionId"] = actId,
                ["name"]             = actName,
                ["type"]             = plugTypeKey,
                ["version"]          = 0,
                ["isContainer"]      = false,
                ["metadata"] = new JsonObject
                {
                    ["designer"] = new JsonObject
                    {
                        ["position"] = new JsonObject { ["x"] = x, ["y"] = y },
                        ["size"]     = new JsonObject { ["width"] = nodeW, ["height"] = nodeH },
                    },
                    ["displayText"] = actName,
                },
            });
        }

        // 连接（相邻节点之间）
        var connections = new JsonArray();
        for (int i = 0; i < ids.Count - 1; i++)
        {
            connections.Add(new JsonObject
            {
                ["source"] = new JsonObject
                {
                    ["activity"] = ids[i],
                    ["port"]     = "Done",
                },
                ["target"] = new JsonObject
                {
                    ["activity"] = ids[i + 1],
                    ["port"]     = "In",
                },
                ["vertices"] = new JsonArray(),
            });
        }

        var root = new JsonObject
        {
            ["id"]           = defId,
            ["nodeId"]       = defId,
            ["definitionId"] = defId,
            ["type"]         = "Elsa.Flowchart",
            ["name"]         = name,
            ["activities"]   = activities,
            ["connections"]  = connections,
        };

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
