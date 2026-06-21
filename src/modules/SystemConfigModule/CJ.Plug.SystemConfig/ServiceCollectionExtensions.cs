using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.SystemConfig.Menus;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.SystemConfig;

public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase { }

    public static IServiceCollection AddSystemConfigPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, SystemConfigMenu>();

        services.AddHttpClient("SystemConfig", client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
