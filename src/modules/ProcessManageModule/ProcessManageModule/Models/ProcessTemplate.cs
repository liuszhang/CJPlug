namespace CJ.Plug.ProcessManage.Models;

/// <summary>
/// 流程模板定义 — 绑定一个完整的 Elsa Flowchart JSON 模板。
/// 创建时通过系统已有的流程导入逻辑将模板 JSON 导入到新流程中。
/// </summary>
public class ProcessTemplate
{
    /// <summary>模板唯一标识</summary>
    public string Id { get; set; } = "";

    /// <summary>模板名称</summary>
    public string Name { get; set; } = "";

    /// <summary>模板描述</summary>
    public string Description { get; set; } = "";

    /// <summary>Material Design 图标名称（如 "Code"）</summary>
    public string Icon { get; set; } = "";

    /// <summary>模板分类标签</summary>
    public string Category { get; set; } = "";

    /// <summary>完整的 Elsa Flowchart JSON 模板（ActivityJsonData 格式）</summary>
    public string TemplateJson { get; set; } = "";
}
