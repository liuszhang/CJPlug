using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CJ.Plug.HostWebServer.Shell;

/// <summary>
/// Blazor 框架资产（`/_framework/*`）的显式端点。
/// </summary>
/// <remarks>
/// <para><b>为什么要有它</b>（2026-10-30 线上故障）：Blazor 启动时会在页面里写
///   <c>&lt;script src="_framework/blazor.web.js"&gt;</c>，这个请求取不到 ⇒ 浏览器
///   <c>Unexpected token '&lt;'</c>、<c>window.Blazor</c> 永远 undefined ⇒ **整站有外观、无事件**
///   （三员登录页点「登录」完全没反应，既不转圈也不报错，看上去像 UI 组件的锅）。
/// </para>
/// <para><b>为什么它取不到</b>：两条供给路径同时失效 ——
///   ① <c>MapStaticAssets()</c> 的清单里没有它（<c>*.staticwebassets.endpoints.json</c> 的
///      <c>_framework</c> 条目数为 0，Debug / Release 都一样）；
///   ② 物理 <c>UseStaticFiles()</c> 在本宿主不生效——它注册在 <c>app.UseRouting()</c> **之后**
///      （<see cref="WebHostStartup.MapPlugWebHost"/> 由 Program.cs 在路由之后调用），
///      实测连临时新建的 <c>wwwroot/__probe.txt</c> 都只能拿到首页 HTML。
///   于是请求落到 Blazor fallback，返回首页 HTML（200 + text/html），症状极具伪装性。
/// </para>
/// <para><b>为什么选这个补法</b>：宿主管道顺序牵一发动全身（认证 / 防伪造 / 内部密钥网关都依赖它），
///   本轮只做加法：用端点直接吐文件，**不动任何中间件顺序**，回归面最小。
///   技术债（根治）是把静态中间件整体挪到 <c>UseRouting()</c> 之前——CJOntology.Web 正是该顺序且实测正常。
/// </para>
/// </remarks>
public static class BlazorFrameworkAssetsEndpoints
{
    /// <summary>
    /// 白名单：Blazor 运行时真正会去取的几个文件（含 sourcemap，便于排障）。
    /// 白名单 + 拒绝任何路径分隔符 ⇒ 无法目录穿越。
    /// </summary>
    private static readonly Dictionary<string, string> Assets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["blazor.web.js"] = "text/javascript",
        ["blazor.server.js"] = "text/javascript",
        ["blazor.webassembly.js"] = "text/javascript",
        ["blazor.web.js.map"] = "application/json",
        ["blazor.server.js.map"] = "application/json",
        ["blazor.webassembly.js.map"] = "application/json",
    };

    /// <summary>把 Blazor 框架资产挂成端点。</summary>
    public static IEndpointConventionBuilder MapBlazorFrameworkAssets(this IEndpointRouteBuilder endpoints)
    {
        // 候选根目录（两级兜底：内容根 / 应用基目录，各自下的 wwwroot/_framework）。
        // 放进委托内部现算，避免 capturing 启动期尚未就绪的环境信息。
        IEnumerable<string> CandidateRoots()
        {
            var roots = new List<string>(4);
            try
            {
                var contentRoot = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(contentRoot)) roots.Add(Path.Combine(contentRoot, "wwwroot", "_framework"));
            }
            catch { /* ignore */ }

            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir)) roots.Add(Path.Combine(baseDir, "wwwroot", "_framework"));

            return roots;
        }

        return endpoints.MapGet("/_framework/{fileName}", async (HttpContext ctx, string fileName) =>
        {
            static bool Bad(string? name) => string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('\\');

            if (Bad(fileName) || !Assets.TryGetValue(fileName, out var contentType))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            foreach (var root in CandidateRoots())
            {
                var full = Path.Combine(root, fileName);
                if (!File.Exists(full)) continue;

                ctx.Response.StatusCode = StatusCodes.Status200OK;
                ctx.Response.ContentType = contentType;
                ctx.Response.ContentLength = new FileInfo(full).Length;
                // 框架资产名带固有哈希后缀（如 blazor.web.xxxx.js 不在白名单内，故此处不加激进缓存）。
                ctx.Response.Headers.CacheControl = "no-cache";
                await ctx.Response.SendFileAsync(full, ctx.RequestAborted);
                return;
            }

            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        });
    }
}
