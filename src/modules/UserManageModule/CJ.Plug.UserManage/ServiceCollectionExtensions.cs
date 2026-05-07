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


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }

    public static IServiceCollection AddUserManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, UserManageMenu>();

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

        return services;
    }

    public static IApplicationBuilder AddUserManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapUserManageApi();
            endpoints.MapRoleManageApi();
            endpoints.MapDepartmentManageApi();
        });
    }
}
