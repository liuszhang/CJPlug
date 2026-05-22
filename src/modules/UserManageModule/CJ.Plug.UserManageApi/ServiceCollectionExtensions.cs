using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.UserManageApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserManageServices(this IServiceCollection services)
    {
        services.AddScoped<IUserManageService, UserManageService>();
        services.AddScoped<IRoleManageService, RoleManageService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IGroupManageService, GroupManageService>();

        return services;
    }
}
