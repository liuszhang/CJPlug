using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
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
                GroupName = MenuGroupEnum.管理.ToString(),
            }
        };
        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}
