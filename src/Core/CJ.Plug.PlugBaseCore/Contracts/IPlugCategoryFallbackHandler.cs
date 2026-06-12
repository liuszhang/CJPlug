namespace CJ.Plug.PlugBaseCore.Contracts;

/// <summary>
/// 按 Category 做回退执行的处理器。
/// 当 PlugTypeKey 无法匹配到内置处理器时，根据插头类别（桌面类/接口类/脚本类）路由到对应的回退处理器。
/// </summary>
public interface IPlugCategoryFallbackHandler : IPlugCommonExecute
{
    /// <summary>该处理器对应的插头类别，如 "桌面类"、"接口类"、"脚本类"</summary>
    string Category { get; }
}
