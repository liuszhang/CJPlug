using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.LlmConfig.Menus;

public class LlmConfigMenu : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.SmartToy,
                Href = "/LlmConfig",
                Text = "LLM 配置",
                GroupName = "管理",
                // P5.5：与页面 [ModuleAccess] 同键 —— 否则"菜单可见"与"页面准入"两套口径会漂移
                AccessKey = "plug-llm-config",
            }
        };
        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}
