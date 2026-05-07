using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.AuditUI
{
    public static class AuditUIServiceCollectionExtensions
    {
        public static IServiceCollection AddAuditModulePageServices(this IServiceCollection services)
        {
            services.AddSingleton<IModule, AuditModule>();
            services.AddScoped<IMenuService, AuditManageMenu>();

            services.AddHttpClient<CJ.Plug.AuditApiClient.IAuditApiClient, CJ.Plug.AuditApiClient.AuditApiClient>(client =>
            {
                client.BaseAddress = new(GlobalData.MainDispatcherServer);
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}
