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

            // P5.5：模块权限声明（矩阵页可见性）
            services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
                new CJCore.Framework.Abstractions.ModuleAccessDeclaration("plug-license", ["system_admin"], "许可证管理"));

            services.AddHttpClient<ILicenseApiClient, global::CJ.Plug.LicenseApiClient.LicenseApiClient>(client =>
            {
                client.BaseAddress = new(GlobalData.MainDispatcherServer);
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}
