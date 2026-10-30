using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using CJ.Plug.Models.Shared;
using CJ.Plug.RelationApi.DbContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{

    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "关系管理";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-relation";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "内核";
    }


    public static IServiceCollection AddRelationManagePageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();

        // P7.1b-B-1：模块权限声明 —— 本模块无菜单（只有 API），此前未声明 ⇒ 模块管理页开关对它
        //   完全无效。补声明后开关才真正可用。
        // ⚠ 无菜单 ⇒ 矩阵分组名回落到 ModuleName，必须传中文显示名。Roles=["*"] = 零可见性回归。
        services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
            new CJCore.Framework.Abstractions.ModuleAccessDeclaration("plug-relation", ["*"], "关系管理"));

        services.AddHttpClient<IRelationApiClient,RelationApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddRelationManageModuleApiServices(this IServiceCollection services)
    {

        services.AddSingleton<IModuleBusinessDbConfig,  RelationModuleDbConfig>();


        services.AddHttpClient<IRelationApiClient, RelationApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddScoped<IRelationManageService, RelationManageService>();
        return services;
    }

    public static IApplicationBuilder AddRelationManageModuleApi(this IApplicationBuilder app)
    {
        return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
        {
            endpoints.MapRelationManageApi();
        });
    }

}

