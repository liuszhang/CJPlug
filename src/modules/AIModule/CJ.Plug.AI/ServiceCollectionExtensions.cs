using CJ.Plug.AI.Pages;
using CJ.Plug.DeekSeekIn;
using CJCore.Framework.Abstractions;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIModuleServices(this IServiceCollection services)
    {
        // ⚠ Singleton 是硬要求（2026-10-29《CJPlug 接入 CJCore》P2-2.8）：
        //   框架根组件 FrameworkApp 用「进程级静态标志」保证模块 InitializeAsync 只执行一次；
        //   注册成 Scoped 会让每个电路新建一份模块实例、重复向单例 IAppBarService 注册 AppBar 项。
        services.AddSingleton<CJCore.Framework.Abstractions.IModule, AIModule>();

        // P5.5：仅给「API Keys 管理页」（/AIAgent）声明子键 ——
        //   模块键 plug-ai 已被 AppBar 的 AskAI 入口占用（AI 助手可见性），不能混用。
        services.AddSingleton<IModuleAccessDeclaration>(
            new ModuleAccessDeclaration("plug-ai:keys", ["system_admin"], "AI 助手"));

        services.AddScoped<IDeepSeekService, DeepSeekService>();
        services.AddAIChatServices();

        return services;
    }


    public class AIModule(IAppBarService appBarService) : ModuleBase
    {
        public override string Name => "AI 助手";

        public override string ModuleId => "plug-ai";

        public override string Group => "通用";

        public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            // AccessKey 与模块 ModuleId 对齐（P6 模块启停 / 功能键门控口径）
            appBarService.AddAppBarItem<AskAI>("plug-ai");
            //Console.WriteLine("Add Ai APPBAR Item.");
            return base.InitializeAsync(cancellationToken);
        }
    }

}

