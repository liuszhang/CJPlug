using CJ.Plug.DispatchServer.Apis;
using CJ.Plug.DispatchServer.Contracts;
using CJ.Plug.DispatchServer.Services;

namespace CJ.Plug.DispatchServer;

/// <summary>
/// 调度面（SignalR Hub + 工位在线表）的注册入口。
/// <para>
/// 2026-10-29《CJPlug 服务架构简化》Phase 2：本文件原先无命名空间（全局类型）。
/// 现在 DispatchServer 已降级为**被 CJ.Plug.ApiServer 引用的类库**（不再作为独立进程启动），
/// 为避免与宿主项目里的全局类型撞名，显式加命名空间、类名改为 DispatchServiceExtensions
/// （扩展方法的调用与类名无关，调用方只需加 using）。
/// </para>
/// <para>
/// 独立部署（回退模式）仍可用：CJ.Plug.DispatchServer/Program.cs 依旧调用这两个方法，
/// 只是 AppHost 默认不再启动该进程。
/// 详见 docs/（待评审）CJPlug服务架构梳理与简化方案-20261029.md
/// </para>
/// </summary>
public static class DispatchServiceExtensions
{
    public static IServiceCollection AddDispatchApiService(this IServiceCollection services)
    {
        services.AddSignalR();

        // ⚠ 宿主可能已注册连接管理器（ApiServer 侧复用同一个实例），这里幂等登记，避免出现第二个连接。
        if (services.All(d => d.ServiceType != typeof(HubConnectionManagerService)))
        {
            services.AddSingleton<HubConnectionManagerService>(new HubConnectionManagerService());
        }

        if (services.All(d => d.ServiceType != typeof(IStationService)))
        {
            services.AddScoped<IStationService, StationService>();
        }

        return services;
    }

    /// <summary>IApplicationBuilder 形态（独立部署的原入口，保持向后兼容）。</summary>
    public static IApplicationBuilder UseDispatchServiceEndpoints(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapDispatchServiceEndpoints();
        });
    }

    /// <summary>
    /// Minimal API 形态的端点映射（合并进宿主后用它，避免在 WebApplication 上再调 UseEndpoints）。
    /// 与 <see cref="UseDispatchServiceEndpoints"/> 语义完全一致。
    /// </summary>
    public static IEndpointRouteBuilder MapDispatchServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<MainHub>("/mainHub");
        endpoints.MapDispatcherApi();
        return endpoints;
    }
}
