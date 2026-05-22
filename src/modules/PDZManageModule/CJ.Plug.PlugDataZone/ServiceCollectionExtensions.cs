using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using CJ.Plug.ApiServer.Services.PDZDatas;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Shared;
using CJ.Plug.PDZModels.DbContext;
using CJ.Plug.PlugDataZoneApiClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PDZManageModule.Permissions;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
    }


    public static IServiceCollection AddPDZManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<IModule, Module>();

        services.AddHttpClient<IPDZApiClient,PDZApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }


    public static IServiceCollection AddPDZManageModuleApiServices(this IServiceCollection services)
    {
        //services.AddDbContext<PDZDbContext>(options =>options.UseSqlite(DbConnectionString.ConnectionString));
        services.AddSingleton<IModuleDbConfig,PDZModuleDbConfig>();

        services.AddScoped<IPDZManageService, PDZManageService>();
        //PDZ数据列表服务
        services.AddScoped<IPlugDataService, PlugDataService>();
        services.AddScoped<IPlugStatusDataService, PlugStatusDataService>();
        services.AddScoped<IFlowchartDataService, FlowchartDataService>();
        services.AddScoped<IDataFlowDataService, DataFlowDataService>();
        services.AddScoped<IPlugVariableDataService, PlugVariableDataService>();

        // 注册功能权限提供者
        services.AddSingleton<IFunctionPermissionProvider, PDZManagePermissionProvider>();

        services.AddHttpClient<IPDZApiClient, PDZApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IApplicationBuilder AddPDZManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPDZManageApi();
            endpoints.MapPlugDataManageApi();
            endpoints.MapFlowchartDataManageApi();
            endpoints.MapDataFlowDataManageApi();
            endpoints.MapPlugVariableDataManageApi();
        });
    }

}

