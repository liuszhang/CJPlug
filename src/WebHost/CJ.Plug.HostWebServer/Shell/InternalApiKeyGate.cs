using System.Collections.Concurrent;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.HostWebServer.Shell;

/// <summary>
/// 内部密钥**入站闸门**（2026-10-29，P5.8 方案 C2 地基）。
///
/// <para><b>三档模式</b>：</para>
/// <list type="table">
/// <item><term>Off</term><description>完全不介入（等价于收紧前的现状）。</description></item>
/// <item><term>Observe</term><description><b>默认</b>。只统计「业务 API 请求里有多少带上了内部密钥」，
///   并对每个首次出现的"缺密钥路径"打一条 Warning。零行为改变，产出收口依据。</description></item>
/// <item><term>EnforceWrite</term><description><b>推荐的上线档</b>。只对<b>写方法</b>
///   （POST/PUT/DELETE/PATCH）缺密钥返 401，读方法（GET/HEAD/OPTIONS/TRACE）仍只统计。
///   这是"改数据"这一真实风险的<b>最小充分收口</b> —— 外部主机能读不能改。</description></item>
/// <item><term>Enforce</term><description>缺密钥的 <c>/api/*</c> 请求直接 401（读写全拦）。
///   ⚠ <b>只有 Observe 模式下<b>写</b>覆盖率 100% 后才能开 EnforceWrite；读覆盖率也 100% 才能开本档</b>，
///   否则会断站。</description></item>
/// </list>
///
/// <para><b>为什么要有 EnforceWrite 这个中间档</b>：全量 Enforce 的爆炸半径包含所有只读调用方
/// （工位机拉工具列表/下载部署包、首页统计等），它们漏接 handler 就会静默变空表；
/// 而写操作几乎全部来自本站内部（页面电路），漏网概率低且一旦漏网立刻可见（操作失败有提示）。
/// 用 EnforceWrite 先把"外部能改数据"这条最危险的通道关掉，再从容补读侧。</para>
///
/// <para><b>为什么先 Observe 再 Enforce</b>：出站 handler 覆盖了 <c>AddHttpClient</c> 的 53 个注册点，
/// 但覆盖不到裸 <c>new HttpClient()</c> 与手工包 DelegatingHandler 的客户端（见
/// <see cref="InternalApiKeyHttpFilter"/> 注释）。必须先用实测数据确认"一条漏网的都没有"，
/// 才允许把统计变成拦截 —— 否则表现是某个模块突然全 404/401，且极难定位到是哪一路调用。</para>
///
/// <para><b>为什么放 UseAuthentication 之后</b>：本中间件只读请求头/路径，不解析主体，
/// 但要排在认证之后才有机会与后续授权口径一致（Enforce 模式下也不会误拦已认证的用户请求）。</para>
/// </summary>
public static class InternalApiKeyGate
{
    public const string ModeOff = "Off";
    public const string ModeObserve = "Observe";
    /// <summary>只拦写方法（POST/PUT/DELETE/PATCH）。</summary>
    public const string ModeEnforceWrite = "EnforceWrite";
    /// <summary>读写全拦。</summary>
    public const string ModeEnforce = "Enforce";

    /// <summary>业务 API 面（待收口）。</summary>
    private const string ApiPrefix = "/api";

    private static long _readWithKey;
    private static long _readWithoutKey;
    private static long _writeWithKey;
    private static long _writeWithoutKey;

    /// <summary>
    /// 缺密钥的路径前缀 → 首次出现的完整路径样例（用于收口清单，不按完整路径聚合以防爆炸）。
    /// <para>⚠ 键带读写维度前缀（<c>W:/api/plug</c> / <c>R:/api/Tool</c>）：同一路径既有读又有写时
    /// 必须分别计数，否则"写覆盖率不达标"会被读侧的高频成功掩盖。</para>
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> MissingSamples = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>统计快照（诊断端点 / 日志用）。</summary>
    public static GateSnapshot Snapshot()
        => new(
            ReadWithKey: Interlocked.Read(ref _readWithKey),
            ReadWithoutKey: Interlocked.Read(ref _readWithoutKey),
            WriteWithKey: Interlocked.Read(ref _writeWithKey),
            WriteWithoutKey: Interlocked.Read(ref _writeWithoutKey),
            Missing: MissingSamples);

    /// <summary>闸门统计快照。<see cref="Missing"/> 的键带 <c>R:</c>/<c>W:</c> 前缀。</summary>
    public readonly record struct GateSnapshot(
        long ReadWithKey,
        long ReadWithoutKey,
        long WriteWithKey,
        long WriteWithoutKey,
        IReadOnlyDictionary<string, string> Missing)
    {
        /// <summary>读方法覆盖率（0~100），无请求时返回 <c>null</c>。</summary>
        public double? ReadCoverage => ReadWithKey + ReadWithoutKey == 0
            ? null
            : ReadWithKey * 100.0 / (ReadWithKey + ReadWithoutKey);

        /// <summary>写方法覆盖率（0~100），无请求时返回 <c>null</c>。</summary>
        public double? WriteCoverage => WriteWithKey + WriteWithoutKey == 0
            ? null
            : WriteWithKey * 100.0 / (WriteWithKey + WriteWithoutKey);
    }

    /// <summary>安全方法（幂等只读）。这类请求在 <see cref="ModeEnforceWrite"/> 下不拦截。</summary>
    private static bool IsSafeMethod(string method)
        => HttpMethods.IsGet(method) || HttpMethods.IsHead(method)
        || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

    public static IApplicationBuilder UseInternalApiKeyGate(
        this IApplicationBuilder app,
        string? mode,
        ILogger? logger = null)
    {
        var normalized = Normalize(mode);

        // Off 或未配置密钥 ⇒ 不挂中间件（零开销、零行为改变）
        if (normalized == ModeOff || !InternalApiKey.IsEnabled)
        {
            return app;
        }

        var gatedMode = normalized;

        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            // 只管业务 API 面。/mcp、/elsa/api/*、/mainHub 按 D-P5-9=A 保持匿名；
            // 三员/个人中心/模块管理端点走认证链（cookie/bearer），不在本机制口径内。
            if (!path.StartsWithSegments(ApiPrefix) || IsExcludedFromGate(path))
            {
                await next();
                return;
            }

            var method = context.Request.Method;
            var isRead = IsSafeMethod(method);
            var dim = isRead ? "R:" : "W:";
            var dimName = isRead ? "读" : "写";

            var presented = context.Request.Headers[InternalApiKey.HeaderName].ToString();
            if (InternalApiKey.Matches(presented))
            {
                if (isRead) Interlocked.Increment(ref _readWithKey);
                else Interlocked.Increment(ref _writeWithKey);
                await next();
                return;
            }

            if (isRead) Interlocked.Increment(ref _readWithoutKey);
            else Interlocked.Increment(ref _writeWithoutKey);

            // 每个「维度+前缀」只记一条（样例保留首个完整路径），避免日志被同一模块刷屏
            var prefix = dim + BuildPrefix(path.Value);
            var isNew = MissingSamples.TryAdd(prefix, $"{method} {path.Value}");
            if (isNew)
            {
                logger?.LogWarning(
                    "[InternalKey] 业务 API 请求未携带内部密钥：{Method} {Path}（{Dim}侧前缀 {Prefix} 首次出现）。" +
                    "若是内部调用方，说明该 HttpClient 未接入 InternalApiKey —— {Advice}",
                    method, path.Value, dimName, prefix,
                    isRead
                        ? "EnforceWrite 档下读侧不拦，但开 Enforce 前必须补齐。"
                        : "写侧已被 EnforceWrite 拦截，必须立刻补齐。");
            }

            // 拦截判定：Enforce 全拦；EnforceWrite 只拦写方法
            var shouldBlock = gatedMode == ModeEnforce || (gatedMode == ModeEnforceWrite && !isRead);
            if (shouldBlock)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "internal-key-required",
                    message = "该 API 仅接受携带内部密钥的进程内/受信调用。",
                    method,
                });
                return;
            }

            await next();
        });
    }

    /// <summary>
    /// 永远排除在闸门之外的路径（不是"漏网"，是<b>明确设计为匿名</b>）：
    /// <list type="bullet">
    /// <item><c>/api/plug/executeMcpTool</c> —— 外部 MCP 客户端调用（D-P5-9=A 保持匿名）；</item>
    /// <item><c>/api/audit/log</c> —— 审计上报，调用量大且已由 AuditApi 自身承载（避免审计链被 401 打断）；</item>
    /// <item><c>/api/diagnostics</c> —— 本机制的诊断端点自身。</item>
    /// </list>
    /// </summary>
    private static bool IsExcludedFromGate(PathString path)
        => path.StartsWithSegments("/api/plug/executeMcpTool", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/api/audit", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/api/diagnostics", StringComparison.OrdinalIgnoreCase)
        // 认证与个人中心自带 cookie/bearer 校验，不需要内部密钥
        || path.StartsWithSegments("/api/three-role", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/api/personal-center", StringComparison.OrdinalIgnoreCase);

    /// <summary>取前两段做聚合键：<c>/api/plug/executePlug/1</c> → <c>/api/plug</c>。避免路径参数撑爆字典。</summary>
    private static string BuildPrefix(string? path)
    {
        if (string.IsNullOrEmpty(path)) return "/";

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "/",
            1 => "/" + parts[0],
            _ => $"/{parts[0]}/{parts[1]}",
        };
    }

    private static string Normalize(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return ModeObserve;
        if (mode.Equals(ModeOff, StringComparison.OrdinalIgnoreCase)) return ModeOff;
        if (mode.Equals(ModeEnforce, StringComparison.OrdinalIgnoreCase)) return ModeEnforce;
        if (mode.Equals(ModeEnforceWrite, StringComparison.OrdinalIgnoreCase)) return ModeEnforceWrite;
        // 拼错时退化为 Observe（不拦），绝不因为配置笔误把全站 401
        return ModeObserve;
    }
}
