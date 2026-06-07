using CJ.Plug.LlmConfigApi.Apis;
using CJ.Plug.LlmConfigApi.Contracts;
using CJ.Plug.LlmConfigApi.Services;
using CJ.Plug.LlmConfigModel.DbContext;
using CJ.Plug.Models.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.LlmConfigApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmConfigModuleApiServices(this IServiceCollection services)
    {
        services.AddSingleton<IModuleDbConfig, LlmConfigDbConfig>();
        services.AddScoped<ILlmConfigService, LlmConfigService>();
        services.AddSingleton<ISeedDataProvider, LlmConfigSeedDataProvider>();
        services.AddHttpClient();
        return services;
    }

    public static IApplicationBuilder AddLlmConfigModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapLlmConfigApi();
        });
    }
}
