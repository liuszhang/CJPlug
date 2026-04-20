using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Shared;
using CJ.Plug.TASApiClient;
using CJ.Plug.ToolActionSettingApi.DbContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManageModule.Menus;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }


    public static IServiceCollection AddTASPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();
        services.AddScoped<IMenuService, ToolActionSettingMenu>();
        services.AddHttpClient<ITASApiClient,TASApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddGuiDesignerService();

        return services;
    }


    


    public static IServiceCollection AddTASModuleApiServices(this IServiceCollection services)
    {
        //services.AddDbContext<TASDbContext>(options =>
        //        options.UseSqlite(DbConnectionString.ConnectionString));

        services.AddSingleton<IModuleDbConfig,TASModuleDbConfig>();

        services.AddScoped<IPlugManageService, PlugManageService>();

        services.AddHttpClient<ITASApiClient, TASApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });


        return services;
    }

    public static IApplicationBuilder AddTASModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPlugManageApi();
        });
    }
    

}

