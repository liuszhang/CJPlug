using CJ.Plug.LicenseApiClient;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace CJ.Plug.LicenseUI
{
    public static class LicenseUIServiceCollectionExtensions
    {
        public static IServiceCollection AddLicenseModulePageServices(this IServiceCollection services)
        {
            services.AddSingleton<CJCore.Framework.Abstractions.IModule, LicenseModule>();
            services.AddScoped<CJCore.Framework.Abstractions.IMenuService, LicenseManageMenu>();

            services.AddHttpClient<ILicenseApiClient, global::CJ.Plug.LicenseApiClient.LicenseApiClient>(client =>
            {
                client.BaseAddress = new(GlobalData.MainDispatcherServer);
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}
