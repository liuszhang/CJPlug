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
            }
        };
        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}
