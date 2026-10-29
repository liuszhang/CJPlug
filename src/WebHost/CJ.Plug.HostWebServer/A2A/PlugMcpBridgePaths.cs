namespace CJ.Plug.HostWebServer.A2A;

/// <summary>
/// CJPlug ↔ McpServer 工具桥的路径常量（<b>两侧共用同一份字面量</b>）。
///
/// <para>宿主侧（本文件）与服务端（<c>CJ.Plug.McpServer/Bridge/PlugMcpBridgeEndpoints.cs</c>）
/// 是两个<b>独立进程</b>且各自编译 ⇒ 路径一旦漂移，表现是"工具清单永远为空"这种静默失败。
/// 集中到一处并配常量引用，是应对本仓"双源漂移"老病的标准做法。</para>
/// </summary>
public static class PlugMcpBridgePaths
{
    /// <summary>工具清单（GET）。</summary>
    public const string ToolsRel = "/api/plugbridge/tools";

    /// <summary>工具执行（POST）。</summary>
    public const string CallRel = "/api/plugbridge/call";
}
