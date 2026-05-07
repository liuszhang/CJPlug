using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.AuthUI
{
    public static class AuthUIServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthModulePageServices(this IServiceCollection services)
        {
            services.AddSingleton<IModule, AuthModule>();
            services.AddScoped<IMenuService, AuthManageMenu>();

            services.AddHttpClient<CJ.Plug.AuthApiClient.IAuthApiClient, CJ.Plug.AuthApiClient.AuthApiClient>(client =>
            {
                client.BaseAddress = new(GlobalData.MainDispatcherServer);
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}
