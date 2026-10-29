using CJ.Plug.LoginPages;
using CJ.Plug.LoginPages.Pages;
using CJCore.Framework.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.LoginPages
{
    /// <summary>
    /// 登录模块：把登录态组件挂到 AppBar。
    /// <para>
    /// 2026-10-29《CJPlug 接入 CJCore》P2-2.7：AppBar 服务改用 CJCore 契约（Singleton），
    /// 注册带 AccessKey（关掉模块时按钮自动消失）。
    /// ⚠ P5 接入 ThreeRoleAdmin 后本模块随登录页一起下线（方案 §6.8 删除清单）。
    /// </para>
    /// </summary>
    public class LoginModule(IAppBarService appBarService) : ModuleBase
    {
        public override string Name => "登录";

        public override string ModuleId => "plug-login";

        public override string Group => "通用";

        public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            appBarService.AddAppBarItem<LoginState>("plug-login");
            return base.InitializeAsync(cancellationToken);
        }
    }
}
