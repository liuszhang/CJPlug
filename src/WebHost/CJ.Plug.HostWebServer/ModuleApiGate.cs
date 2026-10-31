using CJCore.Framework.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;   // ITagsMetadata：读 MapGroup(...).WithTags(...) 设的 Tag
using Microsoft.AspNetCore.Routing;

namespace CJ.Plug.HostWebServer;

/// <summary>
/// P7.1b-B-3：业务 API 的**模块启停门控**（宿主侧集中式）。
/// <para>
/// 背景：B 批次要求"关掉模块后 API 也该拒绝"，但 26 个模块 API 项目**零 CJCore 引用**
/// （<c>RequireModuleAccess</c> 在 <c>CJCore.Framework.Api</c> 里），逐个加引用等于把框架依赖
/// 扩散进全部业务项目。故选 D-B-3=A：在唯一宿主侧集中实现，业务项目一行不改。
/// </para>
/// <para>
/// <b>为什么用端点 Tag 而不是路径前缀做映射</b>：路径前缀存在**一对多冲突** ——
/// <c>/api/plug</c> 同时被三个模块占用（插头执行 / 插头市场 / 插头管理），按路径无法区分归属。
/// 而每个 <c>MapGroup(...)</c> 都调用了 <c>WithTags(...)</c>，Tag 是天然的**模块身份**，
/// 且无需改动任何模块代码即可读取。
/// </para>
/// <para>
/// <b>未命中一律放行</b>（保守口径）：映射表里没有的 Tag 视为"不属于任何可启停模块"，
/// 保持既有匿名可用行为。这样即使映射表有遗漏，后果是"该拦的没拦"，而不是"误伤在用接口"。
/// </para>
/// </summary>
public static class ModuleApiGate
{
    /// <summary>
    /// 端点 Tag → 模块权限键。键必须与 <c>IModule.ModuleId</c> 逐字一致
    /// （<c>IsAccessKeyEnabled</c> 只认登记进 <c>DefaultModuleCatalog</c> 功能索引的键）。
    /// </summary>
    private static readonly Dictionary<string, string> TagToAccessKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["作业管理"]              = "plug-job-manage",
        ["图站管理"]              = "plug-station",
        ["图站配置"]              = "plug-station",
        ["工具管理"]              = "plug-tool-resource",
        ["文件管理"]              = "plug-file-manage",
        ["Guacamole远程桌面"]     = "plug-guacamole",
        ["远程桌面代理"]          = "plug-guacamole",
        ["Station远程桌面状态"]   = "plug-guacamole",
        ["许可证管理"]            = "plug-license",
        ["LLM 配置管理"]          = "plug-llm-config",
        ["MCP管理"]               = "plug-mcptools",
        ["PDZ管理"]               = "plug-pdz",
        ["PDZ管理-Datas"]         = "plug-pdz",
        ["插头执行"]              = "plug-execute",
        ["插头市场"]              = "plug-market",
        ["插头管理"]              = "plug-tool-action",
        ["流程管理"]              = "plug-process-manage",
        ["AI工作流生成"]          = "plug-process-manage",
        ["流程执行(对外)"]        = "plug-to-external",
        ["关系管理"]              = "plug-relation",
        ["Skills管理"]            = "plug-skills-manage",
        // PlugExecuteApi 的 api/systemConfig：独立执行配置（读 StandaloneExecute:AppPath），
        // 与 P7.1a 已退役的 SystemConfig 模块无关，归属插头执行模块。
        ["系统配置"]              = "plug-execute",
        // 审计管理（/api/audit）：AuditModule 无 IModule、无 ModuleId ⇒ 无启停开关，刻意不登记（放行）。
    };

    /// <summary>Observe 模式下已告警过的 Tag（首例留痕，避免刷屏；口径同 P5.8 内部密钥闸门）。</summary>
    private static readonly HashSet<string> _observed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Observe 模式下未命中映射表的 Tag（首例留痕，用于发现漏登记的新模块）。</summary>
    private static readonly HashSet<string> _unknownTags = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 挂载业务 API 门控。
    /// </summary>
    /// <param name="mode">Off = 完全不挂（零开销）；Observe = 只记日志不拦截；Enforce = 真拦（403）。配置拼错/为空按 Observe。</param>
    public static IApplicationBuilder UseModuleApiGate(this IApplicationBuilder app, string? mode, ILogger logger)
    {
        if (string.Equals(mode, "Off", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[ModuleApiGate] Mode=Off ⇒ 不挂载（业务 API 无任何启停门控）");
            return app;
        }

        var enforce = string.Equals(mode, "Enforce", StringComparison.OrdinalIgnoreCase);
        logger.LogInformation("[ModuleApiGate] 已挂载，Mode={Mode}（映射 {Count} 个 Tag → 模块键）",
            enforce ? "Enforce" : "Observe", TagToAccessKey.Count);

        return app.Use(async (ctx, next) =>
        {
            // 只在业务 API 面生效：页面由 [ModuleAccess] + ModuleAccessRouteView 负责，此处不重复处理。
            var endpoint = ctx.GetEndpoint();
            var tags = endpoint?.Metadata.GetMetadata<ITagsMetadata>()?.Tags;
            if (tags is null || tags.Count == 0)
            {
                await next();
                return;
            }

            string? accessKey = null;
            foreach (var tag in tags)
            {
                if (tag is not null && TagToAccessKey.TryGetValue(tag, out accessKey)) break;
            }

            if (accessKey is null)
            {
                // 未命中映射表：记首例（供补齐映射），但一律放行。
                if (_unknownTags.Add(tags.First() ?? ""))
                {
                    logger.LogInformation("[ModuleApiGate] 未登记 Tag「{Tag}」⇒ 放行（若它属于某模块，请补进 TagToAccessKey）",
                        tags.First());
                }
                await next();
                return;
            }

            var gate = ctx.RequestServices.GetService<IModuleEnablementGate>();
            if (gate is null)
            {
                // 没有门控服务（理论上不会）⇒ 放行，绝不因基础设施缺失而拒绝业务流量。
                await next();
                return;
            }

            var enabled = gate.IsAccessKeyEnabled(accessKey);
            if (enabled)
            {
                await next();
                return;
            }

            if (!enforce)
            {
                if (_observed.Add(accessKey))
                {
                    logger.LogWarning("[ModuleApiGate] Observe：模块键「{Key}」已停用，但请求仍放行 —— {Method} {Path}",
                        accessKey, ctx.Request.Method, ctx.Request.Path);
                }
                await next();
                return;
            }

            logger.LogInformation("[ModuleApiGate] Enforce：模块键「{Key}」已停用 ⇒ 403 {Method} {Path}",
                accessKey, ctx.Request.Method, ctx.Request.Path);
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.WriteAsync(
                $"{{\"error\":\"模块已停用\",\"accessKey\":\"{accessKey}\",\"path\":\"{ctx.Request.Path}\"}}");
        });
    }
}
