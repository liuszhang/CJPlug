using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlugExecutePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();

        services.AddHttpClient<IExecuteApiClient,ExecuteApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }


    public class Module : ModuleBase
    {
    }

    public static IServiceCollection AddPlugExecuteModuleApiServices(this IServiceCollection services)
    {
        //添加插头执行服务
        services.AddScoped<IPlugCommonExecute>(sp => new DefaultPlugExecuteService(sp, sp.GetRequiredService<IToolExecuteService>()));

        services.AddScoped<IPlugExecuteService, PlugExecuteService>();

        services.AddHttpClient<IExecuteApiClient,ExecuteApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IApplicationBuilder AddPlugExecuteModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPlugExecuteApi();
        });
    }

}

