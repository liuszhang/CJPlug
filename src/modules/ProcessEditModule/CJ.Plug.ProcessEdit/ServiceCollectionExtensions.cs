using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcessEditPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();

        return services;
    }


    public class Module : ModuleBase
    {
    }

}

