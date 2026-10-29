using CJCore.Modules.ThreeRoleAdmin;
using CJCore.Modules.ThreeRoleAdmin.Dtos;
using Microsoft.AspNetCore.Components;

namespace CJ.Plug.HomePage.Components.Landing;

public partial class StatisticsLine
{
    private int _userCount;
    private int _toolCount;
    private int _plugCount;
    private string _version = "1.0.0";
    private bool _loaded;

    [Inject] private MainApiClient ApiClient { get; set; } = null!;

    /// <summary>
    /// P5.6：用户统计口径切到三员体系（TRA_Users）。
    /// 旧 <c>ApiClient.GetAllUsersAsync()</c> 随 UserManageModule 一并删除。
    /// Landing 页允许匿名访问 ⇒ 未登录时该调用会 401，由外层 catch 兜底为 0。
    /// </summary>
    [Inject] private IThreeRoleAdminApiClient ThreeRoleApi { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            try
            {
                var users = await ThreeRoleApi.GetUsersAsync(new UserQueryRequest { Page = 1, PageSize = 1 });
                _userCount = users?.TotalCount ?? 0;
            }
            catch
            {
                // 匿名访问首页时无权限读用户列表 ⇒ 保持 0，不打断其余统计
                _userCount = 0;
            }

            var tools = await ApiClient.GetAllToolsAsync();
            _toolCount = tools?.Count ?? 0;

            var plugs = await ApiClient.GetPlugs();
            _plugCount = plugs?.Count ?? 0;

            _loaded = true;
        }
        catch
        {
            _loaded = true;
        }
    }

}
