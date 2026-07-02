using CJ.Plug.LicenseApi.Apis;
using CJ.Plug.LicenseApi.Contracts;
using CJ.Plug.LicenseApi.DbContext;
using CJ.Plug.LicenseApi.Services;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CJ.Plug.LicenseApi
{
    public static class LicenseServiceCollectionExtensions
    {
        public static IServiceCollection AddLicenseModuleApiServices(this IServiceCollection services)
        {
            services.AddSingleton<IModuleDbConfig, LicenseModuleDbConfig>();
            services.AddSingleton<ILicenseService, LicenseService>();

            // 为码支付 API 调用注册 HttpClient（如宿主已注册则跳过）
            services.TryAddSingleton(_ => new HttpClient());

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
