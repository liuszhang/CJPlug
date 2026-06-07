using CJ.Plug.ModelManageApi.Apis;
using CJ.Plug.ModelManageApi.Contracts;
using CJ.Plug.ModelManageApi.Services;
using CJ.Plug.ModelManageModel.DbContext;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.ModelManageApi
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOntologyManageModuleApiServices(this IServiceCollection services)
        {
            services.AddSingleton<IModuleDbConfig, OntologyManageDbConfig>();
            services.AddScoped<IOntologyManageService, OntologyManageService>();
            services.AddSingleton<ISeedDataProvider, OntologyManageSeedDataProvider>();
            return services;
        }

        public static IApplicationBuilder AddOntologyManageModuleApi(this IApplicationBuilder app)
        {
            return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {
                endpoints.MapOntologyManageApi();
            });
        }
    }
}
