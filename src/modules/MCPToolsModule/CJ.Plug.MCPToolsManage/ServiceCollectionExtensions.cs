using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.MCPToolApiClient;
using CJ.Plug.MCPToolsManage.Menus;
using CJ.Plug.MCPToolsManageApi.Apis;
using CJ.Plug.MCPToolsManageApi.Contracts;
using CJ.Plug.MCPToolsManageApi.DbContext;
using CJ.Plug.MCPToolsManageApi.Services;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "MCP 工具管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-mcptools";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "集成";
    }


    //前端服务包
    public static IServiceCollection AddMCPToolsModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, MCPToolsManageMenu>();

        // P5.5：模块权限声明（矩阵页可见性）
        services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
            new CJCore.Framework.Abstractions.ModuleAccessDeclaration("plug-mcptools", ["system_admin"], "MCP 工具管理"));

        services.AddHttpClient<IMCPToolApiClient, MCPToolApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    //后端API服务包
    public static IServiceCollection AddMCPToolsApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleBusinessDbConfig,  MCPToolsManageDbConfig>();

        services.AddScoped<IMCPToolsManageService, MCPToolsManageService>();

        // 注册 MCP Tool 变更通知器（通过 HTTP 调用 DispatchServer 触发 SignalR 广播）
        services.AddSingleton<IMcpToolChangeNotifier, HttpMcpToolChangeNotifier>();
        services.AddHttpClient("DispatchServer", client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<IMCPToolApiClient, MCPToolApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    //后端API路由注册
    public static IApplicationBuilder AddMCPToolsApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMCPToolsManageApi();
        });
    }
}
