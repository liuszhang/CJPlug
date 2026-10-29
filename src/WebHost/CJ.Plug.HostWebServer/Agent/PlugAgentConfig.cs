using CJCore.Agent.Abstractions;

namespace CJ.Plug.HostWebServer.Agent;

/// <summary>
/// CJPlug 工作流 Agent 的配置（<see cref="IAgentConfig"/> 实现，P8.3.3）。
///
/// <para><b>SystemPrompt 为什么这么写</b>：本 Agent 的"工具"是<b>平台里已发布的工作流</b>，
/// 模型只能看到 <c>name + description + 参数 schema</c>，看不到工作流内部干了什么。
/// 因此提示词必须显式交代三件事：① 工具清单即能力边界；② 参数拿不准就先问，不要靠猜填；
/// ③ 会触发真实执行，不要为了"显得有用"而调用。</para>
///
/// <para><b>Model = null</b>：用平台默认模型（<see cref="PlugLlmConfigReader"/> 给的那一个），
/// 避免 Agent 这一路又开一份模型选择。</para>
///
/// <para><b>MaxToolCallRounds = 8</b>（刻意给显式上限）：基类语义是「≤0 = 不限轮次，
/// 直到模型不再要工具」——对一个会真实驱动工位机的工具面来说，不限轮次 = 成本与副作用都不可控。
/// 8 轮足够完成"查 → 决策 → 执行 → 复核"，不够说明任务该拆，而不是让循环继续跑。</para>
/// </summary>
public sealed class PlugAgentConfig : IAgentConfig
{
    /// <summary>Agent 名：出现在日志、Agent 目录与提示词里，保持全仓唯一。</summary>
    public string AgentName => "plug-workflow-agent";

    public string Description => "用 CJPlug 平台已发布的工作流完成用户任务（会触发真实执行）";

    public string SystemPrompt => """
        你是 CJPlug 插座平台的任务助手。

        你的能力边界 = 可用工具清单；每个工具对应平台里一条**已发布的工作流**。

        规则（务必遵守）：
        1. 只调用工具清单里存在的工具，不要臆造工具名。
        2. 参数拿不准时先向用户确认，不要凭猜测填参数 —— 工具调用会触发**真实执行**，猜错参数是有代价的。
        3. 若用户的需求不在工具能力范围内，直接说明做不到，不要用"我觉得可以"来替代执行。
        4. 用完工具后，用中文简要说明：调用了哪个工具、结果是什么、下一步建议。
        """;

    /// <summary>null = 用平台默认模型（见类注释）。</summary>
    public string? Model => null;

    public double Temperature => 0.2;

    /// <summary>显式轮次上限（见类注释）。</summary>
    public int MaxToolCallRounds => 8;
}
