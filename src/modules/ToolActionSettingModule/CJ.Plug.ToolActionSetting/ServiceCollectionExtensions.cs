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
using ToolActionSettingModule.Permissions;


public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "工具动作配置";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-tool-action";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "业务";
    }



    public static IServiceCollection AddTASPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, ToolActionSettingMenu>();
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

        // 注册功能权限提供者
        services.AddSingleton<IFunctionPermissionProvider, ToolActionSettingPermissionProvider>();

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

