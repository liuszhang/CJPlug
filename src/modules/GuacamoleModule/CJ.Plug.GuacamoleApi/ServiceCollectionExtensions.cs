using CJ.Plug.GuacamoleApi.Apis;
using CJ.Plug.GuacamoleApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.GuacamoleApi
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册远程桌面代理服务 (服务端)
        /// </summary>
        public static IServiceCollection AddGuacamoleApiServices(this IServiceCollection services)
        {
            // 注册 WebSocket 代理服务
            services.AddSingleton<VncWebSocketProxy>();
            services.AddSingleton<SshWebSocketProxy>();
            services.AddSingleton<CaptureWebSocketProxy>();

            return services;
        }

        /// <summary>
        /// 注册远程桌面 API 端点
        /// </summary>
        public static IApplicationBuilder AddGuacamoleApi(this IApplicationBuilder app)
        {
            return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {
                endpoints.MapRemoteDesktopApi();
                endpoints.MapStationRemoteStatusApi();
            });
        }
    }
}
