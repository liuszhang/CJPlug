using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.HostWebServer.Agent;

/// <summary>
/// 「工作流 Agent（调试）」菜单项 —— 挂 <b>调试</b> 组（D3=A 归一后的三组之一）。
///
/// <para><b>为什么挂调试而不是通用</b>：这是 P8 消费链的<b>验收入口</b>，不是面向终端用户的功能
/// （D2=A 的口径：入口收敛）。放调试组既方便找，也避免把它当成"产品已交付的 AI 助手"。</para>
///
/// <para><b>为什么不挂 Roles / AccessKey</b>：与 P6 / P7.1 同一教训 —— P5 之前用户恒 Admin，
/// 挂角色约束出现过"菜单项查无此项"的假象；P5 接 ThreeRoleAdmin + 权限矩阵后再回来补。</para>
/// </summary>
public sealed class PlugAgentMenuService : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        MenuItem menuItem = new()
        {
            Icon = Icons.Material.Filled.SmartToy,
            Href = "/debug/plug-agent",
            Text = "工作流 Agent",
            GroupName = "调试",
            Order = 60,
        };

        return ValueTask.FromResult<IEnumerable<MenuItem>>([menuItem]);
    }
}
