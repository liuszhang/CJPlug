using CJ.Plug.LlmConfig.Menus;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;
using ILlmConfigApiClient = CJ.Plug.LlmConfigApiClient.ILlmConfigApiClient;
using LlmConfigApiClientImpl = CJ.Plug.LlmConfigApiClient.LlmConfigApiClient;

namespace CJ.Plug.LlmConfig;

public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase { }

    public static IServiceCollection AddLlmConfigPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, LlmConfigMenu>();

        services.AddHttpClient<ILlmConfigApiClient, LlmConfigApiClientImpl>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
