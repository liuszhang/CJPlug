using CJ.Plug.AuditApi.Apis;
using CJ.Plug.AuditApi.Contracts;
using CJ.Plug.AuditApi.DbContext;
using CJ.Plug.AuditApi.Services;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.AuditApi
{
    public static class AuditServiceCollectionExtensions
    {
        public static IServiceCollection AddAuditModuleApiServices(this IServiceCollection services)
        {
            services.AddSingleton<IModuleDbConfig, AuditModuleDbConfig>();
            services.AddScoped<IAuditService, AuditService>();

            return services;
        }

        public static IApplicationBuilder AddAuditModuleApi(this IApplicationBuilder app)
        {
            return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {
                endpoints.MapAuditApi();
            });
        }
    }
}
