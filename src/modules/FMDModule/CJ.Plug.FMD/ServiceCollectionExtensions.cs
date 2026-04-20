using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }

    public static IServiceCollection AddFMDModuleServices(this IServiceCollection services)
    {
        services.AddScoped<IModule, Module>();
        services.AddScoped<IMenuService, FMDMenu>();

        return services;
    }


    

}

