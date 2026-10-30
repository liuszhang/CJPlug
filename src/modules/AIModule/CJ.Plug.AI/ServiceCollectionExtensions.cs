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

        // P5.5：给「API Keys 管理页」（/AIAgent）声明子键 ——
        //   模块级键 plug-ai 专用于 AppBar 的 AskAI 入口（AI 助手可见性），二者分属不同用途，不可合并。
        services.AddSingleton<IModuleAccessDeclaration>(
            new ModuleAccessDeclaration("plug-ai:keys", ["system_admin"], "AI 助手"));

        // P6.4：补声明模块级键 plug-ai —— AppBar 的 AskAI 入口用的就是这个键（见下方 InitializeAsync）。
        //   此前只声明了 plug-ai:keys 子键，plug-ai 未进 DefaultModuleCatalog._accessKeyIndex，
        //   而闸门对「未登记键」直接放行 ⇒ 关 plug-ai 模块对 AskAI 完全无效（静默失效）。
        //   两条声明同属 plug-ai 模块，关模块时一起失效，语义正确。
        //   Roles=["*"] = 公共项（所有已登录用户命中），保持 AppBar 原有「仅登录可见」口径不变（D-P6-4=A）。
        services.AddSingleton<IModuleAccessDeclaration>(
            new ModuleAccessDeclaration("plug-ai", ["*"], "AI 助手"));

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

