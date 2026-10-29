using CJ.Plug.AuthApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Apis;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageApi.DbContext;
using CJ.Plug.UserManageApi.Services;
using CJ.Plug.UserManageApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;
using UserManageModule.Permissions;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "用户管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-user-manage";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "内核";
    }

    public static IServiceCollection AddUserManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, UserManageMenu>();

        services.AddHttpClient<IUserManageApiClient, UserManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IRoleManageApiClient, RoleManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IDepartmentManageApiClient, DepartmentManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IRolePermissionApiClient, RolePermissionApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IGroupManageApiClient, GroupManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // 注册AuthApiClient用于授权管理
        services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddUserManageModuleApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, UserModuleDbConfig>();

        services.AddScoped<IUserManageService, UserManageService>();
        services.AddScoped<IRoleManageService, RoleManageService>();
        services.AddScoped<IDepartmentManageService, DepartmentManageService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IGroupManageService, GroupManageService>();

        // 注册功能权限提供者
        services.AddSingleton<IFunctionPermissionProvider, UserManagePermissionProvider>();

        // 注册种子数据提供者
        services.AddSingleton<ISeedDataProvider, UserManageSeedDataProvider>();

        services.AddHttpClient<IUserManageApiClient, UserManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IRoleManageApiClient, RoleManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IDepartmentManageApiClient, DepartmentManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IRolePermissionApiClient, RolePermissionApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IGroupManageApiClient, GroupManageApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IApplicationBuilder AddUserManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapUserManageApi();
            endpoints.MapRoleManageApi();
            endpoints.MapDepartmentManageApi();
            endpoints.MapRolePermissionApi();
            endpoints.MapGroupManageApi();
        });
    }
}
