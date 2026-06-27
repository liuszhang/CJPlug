using CJ.Plug.LicenseApi.Apis;
using CJ.Plug.LicenseApi.Contracts;
using CJ.Plug.LicenseApi.DbContext;
using CJ.Plug.LicenseApi.Services;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.LicenseApi
{
    public static class LicenseServiceCollectionExtensions
    {
        public static IServiceCollection AddLicenseModuleApiServices(this IServiceCollection services)
        {
            services.AddSingleton<IModuleDbConfig, LicenseModuleDbConfig>();
            services.AddSingleton<ILicenseService, LicenseService>();

            return services;
        }

        public static IApplicationBuilder AddLicenseModuleApi(this IApplicationBuilder app)
        {
            return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {
                endpoints.MapLicenseApi();
            });
        }
    }
}
