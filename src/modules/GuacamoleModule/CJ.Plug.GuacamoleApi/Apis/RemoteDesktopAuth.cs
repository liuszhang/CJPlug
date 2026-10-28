using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CJ.Plug.GuacamoleApi.Apis
{
    /// <summary>
    /// 远程桌面 WS 代理端点鉴权（子方案1 D6）。
    /// 校验顺序：
    /// 1) 平台用户认证（cookie/JWT）已认证 → 通过；
    /// 2) 配置 RemoteDesktop:AccessToken 非空时，要求 ?token= 匹配；
    /// 3) 未配置 AccessToken 且无平台认证 → 放行（兼容现状，由部署层网络策略兜底）。
    /// </summary>
    public static class RemoteDesktopAuth
    {
        public static bool IsAuthorized(HttpContext context, IConfiguration configuration)
        {
            // 1) 平台用户认证（若 ApiServer 挂载了认证中间件，同源 WS 握手会带 cookie）
            if (context.User?.Identity?.IsAuthenticated == true)
                return true;

            // 2) 部署级共享令牌
            var accessToken = configuration["RemoteDesktop:AccessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                var provided = context.Request.Query["token"].FirstOrDefault();
                return !string.IsNullOrEmpty(provided) &&
                       string.Equals(provided, accessToken, StringComparison.Ordinal);
            }

            // 3) 未配置 → 放行
            return true;
        }
    }
}
