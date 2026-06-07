using CJ.Plug.ModelManage.Menus;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;
using IOntologyManageApiClient = CJ.Plug.ModelManageApiClient.IOntologyManageApiClient;
using OntologyManageApiClientImpl = CJ.Plug.ModelManageApiClient.OntologyManageApiClient;

namespace CJ.Plug.ModelManage
{
    public static class ServiceCollectionExtensions
    {
        public class Module : ModuleBase { }

        public static IServiceCollection AddOntologyManagePageModuleServices(this IServiceCollection services)
        {
            services.AddSingleton<IModule, Module>();
            services.AddScoped<IMenuService, OntologyManageMenu>();

            services.AddHttpClient<IOntologyManageApiClient, OntologyManageApiClientImpl>(client =>
            {
                client.BaseAddress = new(GlobalData.MainDispatcherServer);
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}
