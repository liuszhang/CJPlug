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
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "插头数据区(PDZ)";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-pdz";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
    }


    public static IServiceCollection AddPDZManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();

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
        services.AddSingleton<IModuleBusinessDbConfig, PDZModuleDbConfig>();

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

