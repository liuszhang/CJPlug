using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJ.Plug.AuditUI
{
    public class AuditManageMenu : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
        {
            var menuItems = new List<MenuItem>
            {
                new()
                {
                    Icon = Icons.Material.Filled.History,
                    Href = "/AuditManage",
                    Text = "审计管理",
                    GroupName = "管理",
                }
            };

            return new ValueTask<IEnumerable<MenuItem>>(menuItems);
        }
    }

    public class AuditModule : CJ.Plug.Models.Abstractions.ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "审计管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-audit";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "内核";
    }
}
