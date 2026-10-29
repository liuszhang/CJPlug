using CJCore.Agent.Abstractions;
using CJCore.AgentTool.MCPTools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CJ.Plug.HostWebServer.Agent;

/// <summary>
/// CJPlug 的 <b>工作流 Agent</b>（P8.3.4）—— 第一个真正消费 <see cref="IToolRegistry"/> 的通路。
///
/// <para><b>本类刻意很薄</b>：多轮循环、工具结果回填、轮次上限、危险工具中断（fail-closed）
/// 全在 <see cref="LLMAgentBase{TConfig}"/> 里。这里只做两件事：装一次 <see cref="AgentRequest"/>、
/// 把 <see cref="AgentResponse"/> 交回调用方。这是 C 分支选"继承内核基类"而不是再手搓一遍循环的理由。</para>
///
/// <para><b>为什么是 Singleton</b>：基类注释写明它"生命周期无关"（每次 ExecuteAsync 内按调用建 scope
/// 解析 <c>ILLMChatService</c>）⇒ 注册成 Singleton 既安全又能常驻；本 Agent 无可变状态。</para>
///
/// <para><b>不注入 ISessionStore 的含义</b>：每次调用都是独立会话，不保留跨次历史。
/// 与 <c>SystemMonitorAnalysisAgent</c> 的做法一致 —— 本阶段先不要"记住上一次说过什么"，
/// 那会引入一套会话持久化与清理责任。</para>
///
/// ⚠ <b>安全口径（D2=A）</b>：工具的 <c>IsDangerous</c> 当前为 <c>false</c>（可直接执行），
/// 因此安全边界不在这个类里，而在<b>入口收敛</b>：只有调试页能触发本 Agent，
/// 且页面要求操作者显式确认"已确认工作流副作用"后才放开执行。审批门控留待 P8.4。
/// </summary>
public sealed class PlugWorkflowAgent : LLMAgentBase<PlugAgentConfig>
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PlugWorkflowAgent> _logger;

    public PlugWorkflowAgent(
        PlugAgentConfig config,
        IToolRegistry toolRegistry,
        IServiceScopeFactory scopeFactory,
        ILogger<PlugWorkflowAgent> logger)
        : base(config, toolRegistry, scopeFactory, logger)
    {
        _scopes = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 跑一次会话（一句话进，一段答复出）。
    /// </summary>
    /// <param name="question">用户问题（自然语言）。</param>
    /// <param name="ct">取消令牌。</param>
    public Task<AgentResponse> RunAsync(string question, CancellationToken ct = default)
    {
        // 每次一个稳定 id：日志里能一眼对上"是哪一次提问"，也让每轮工具调用可追溯。
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        _logger.LogInformation("[P8-Agent] 会话开始 id={CorrelationId}, 工具数={ToolCount}",
            correlationId, _toolRegistry.AllEnabledDefinitions.Count);

        return ExecuteAsync(
            new AgentRequest
            {
                Text = question,
                TargetAgent = Name,
                CorrelationId = correlationId,
                SessionId = correlationId,   // 未注入 ISessionStore ⇒ 该值不产生跨次历史
            },
            new DefaultAgentContext(correlationId, _logger, _scopes, ct));
    }

    /// <summary>
    /// 当前喂给模型的工具清单（<b>只读</b>，供调试页与 <c>/api/plug-agent/tools</c> 展示）。
    ///
    /// <para>刻意走 <see cref="IToolRegistry.AllEnabledDefinitions"/>：它 = static ∪ dynamic 且只按
    /// <c>Enabled</c> 过滤 ⇒ 页面看到的<b>就是模型看到的</b>，避免"页面显示有、模型却看不到"这类偏差。</para>
    /// </summary>
    public IReadOnlyList<CJCore.LLM.Abstractions.ToolDefinition> GetVisibleTools()
        => _toolRegistry.AllEnabledDefinitions;
}
