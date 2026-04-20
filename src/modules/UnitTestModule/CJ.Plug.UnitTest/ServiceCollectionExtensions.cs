using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUnitTestPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, UnitTestMenu>();

        return services;
    }


    public class Module : ModuleBase
    {
    }

}

