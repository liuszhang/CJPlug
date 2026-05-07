using CJ.Plug.AuthApi.Apis;
using CJ.Plug.AuthApi.Contracts;
using CJ.Plug.AuthApi.DbContext;
using CJ.Plug.AuthApi.Services;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.AuthApi
{
    public static class AuthServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthModuleApiServices(this IServiceCollection services)
        {
            services.AddSingleton<IModuleDbConfig, AuthModuleDbConfig>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }

        public static IApplicationBuilder AddAuthModuleApi(this IApplicationBuilder app)
        {
            return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {
                endpoints.MapAuthApi();
            });
        }
    }
}
