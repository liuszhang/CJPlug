using CJ.Plug.LlmConfig.Menus;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.DependencyInjection;
using ILlmConfigApiClient = CJ.Plug.LlmConfigApiClient.ILlmConfigApiClient;
using LlmConfigApiClientImpl = CJ.Plug.LlmConfigApiClient.LlmConfigApiClient;

namespace CJ.Plug.LlmConfig;

public static class ServiceCollectionExtensions
{
    public class Module : ModuleBase
    {
        /// <summary>模块显示名（P6.1：取代 D2 过渡期的「命名空间末段」取值）。</summary>
        public override string Name => "LLM 配置";

        /// <summary>稳定标识键（Q8 定案 <c>plug-&lt;模块&gt;</c>），模块启停状态的持久化键。</summary>
        public override string ModuleId => "plug-llm-config";

        /// <summary>模块管理页分组。</summary>
        public override string Group => "集成";
    }

    public static IServiceCollection AddLlmConfigPageModuleServices(this IServiceCollection services)
    {
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, Module>();
        services.AddScoped<CJCore.Framework.Abstractions.IMenuService, LlmConfigMenu>();

        // P5.5：模块权限声明 —— 供权限矩阵页（/admin/role-permissions）扫描出该键；
        //   不注册则矩阵里看不到它，现场无法改角色（只能改代码重编）。
        services.AddSingleton<CJCore.Framework.Abstractions.IModuleAccessDeclaration>(
            new CJCore.Framework.Abstractions.ModuleAccessDeclaration("plug-llm-config", ["system_admin"], "LLM 配置"));

        services.AddHttpClient<ILlmConfigApiClient, LlmConfigApiClientImpl>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
