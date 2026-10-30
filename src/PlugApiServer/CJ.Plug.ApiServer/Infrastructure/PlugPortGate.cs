namespace CJ.Plug.ApiServer.Infrastructure;

/// <summary>
/// 端口级路由闸门（2026-10-29《CJPlug 服务架构简化》Phase 4 引入）。
/// <para>
/// 合并后**一个进程监听多个端口**。Kestrel 的多个 EndPoint 共享同一张路由表，
/// 若不拦截，3001（对外 MCP 面）会顺带暴露全部业务 API，8687 会暴露 /elsa/api 与 MCP 面——
/// 这是合并带来的真实安全面扩大。
/// </para>
/// <para>
/// 这里按「端口 = 对外契约」做白名单，保证**合并前后每个端口能看到的路径完全一致**：
/// <list type="bullet">
/// <item>8686（Dispatch）：仅 /mainHub 与 /api/dispatch/*（工位机、日志上报、Hub 客户端）</item>
/// <item>8687（主 API）：除 MCP 面与 /elsa/* 之外全部放行（保持既有 27 个模块端点不变）</item>
/// <item>5001（Elsa）：仅 /elsa/*（Elsa Studio 与 ElsaApiClient 使用）</item>
/// <item>3001（MCP）：仅 MCP 协议面与 /api/plugbridge/*（对外 AI 客户端与 CJCore 工具桥）</item>
/// <item>5066（Web）：同 8687 —— 合并后由同一进程提供页面，但不额外暴露 MCP 面与 Elsa 面</item>
/// </list>
/// </para>
/// <para>
/// ⚠ 2026-10-29 第二轮（单端口收敛）后本中间件会被整体移除：只剩 5066 一个端口时，
///   端口不再具备区分能力，安全边界回落到路径（CJPlug 本来无鉴权，故不构成能力降级）。
///   移除时同时删掉 Program.cs 里的 <c>app.UsePlugPortGate()</c> 与 3001 根路径重写。
/// </para>
/// </summary>
public static class PlugPortGate
{
    public const int DispatchPort = 8686;
    public const int ApiPort = 8687;
    public const int ElsaPort = 5001;
    public const int McpPort = 3001;
    public const int WebPort = 5066;

    public static IApplicationBuilder UsePlugPortGate(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (!IsAllowed(context.Connection.LocalPort, context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });

    public static bool IsAllowed(int port, PathString path)
    {
        switch (port)
        {
            case DispatchPort:
                // SignalR 的协商与传输都挂在 /mainHub 下（/mainHub/negotiate 等）。
                return path.StartsWithSegments("/mainHub")
                    || path.StartsWithSegments("/api/dispatch");

            case ElsaPort:
                return path.StartsWithSegments("/elsa");

            case McpPort:
                // ⚠ 2026-10-30（P9.1）：**McpPort(3001) 这个分支在唯一宿主形态下已不再被触发**
                // —— 进程只监听 5066，MCP 面是 5066 上的 /mcp（原注释写的
                // "根路径仍放行，请求被重写中间件转到 /mcp"是旧独立进程形态的口径）。
                //   本行保留是为了 switch 完备性与将来真要开第二端口时不用重写；
                //   实际门控由 IsMcpEndpoint + /api/plugbridge 承担，根路径 "/" 现在属于 Blazor。
                return path == "/"
                    || IsMcpEndpoint(path)
                    || path.StartsWithSegments("/api/plugbridge");

            case ApiPort:
            case WebPort:
                // 主 API 与 Web 端口：保持既有行为，只是不额外暴露 MCP 面与 Elsa 面。
                return !IsMcpEndpoint(path) && !path.StartsWithSegments("/elsa");

            default:
                // 未纳入规划的端口（如 Aspire 健康检查、调试端口）不做限制。
                return true;
        }
    }

    /// <summary>
    /// MCP 协议面路径。
    /// <para>
    /// ⚠ 2026-10-29 第二轮：MCP 从「根路径」改挂 <c>/mcp</c> —— 因为合并 Blazor 宿主后根路径被首页占用，
    /// 同 pattern 两个端点会抛 <c>AmbiguousMatchException</c>（实测原 3001 根路径 GET 返回 405，
    /// 说明端点确实匹配在 "/" 上）。<c>/sse</c> 与 <c>/message</c> 是旧 SSE 传输的路径，一并保留。
    /// </para>
    /// </summary>
    public static bool IsMcpEndpoint(PathString path)
        => path.StartsWithSegments("/mcp")
           || path.StartsWithSegments("/sse")
           || path.StartsWithSegments("/message");
}
