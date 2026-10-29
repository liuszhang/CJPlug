using CJ.Plug.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.Models.Services;

/// <summary>
/// 把 CJPlug 的 18 个业务 <see cref="IModuleBusinessDbConfig"/> 汇总成<b>一个</b>
/// CJCore <see cref="CJCore.Modules.Data.IModuleDbConfig"/>，注册进 DI 供 <c>DataDbContext</c> 使用。
/// </summary>
/// <remarks>
/// <para><b>为什么需要适配器</b>（2026-10-29 P4）：MS DI 不把派生接口的注册解析成基接口。
/// 若把 18 个配置逐个改注册成 CJCore 的接口，则 18 个模块项目都得新增对 CJCore.Modules.Data 的
/// 项目引用（<c>using CJCore.Modules.Data;</c> 才能写出服务类型）；用适配器则只需
/// <c>CJ.Plug.Models</c> 一个项目引用 CJCore，18 个模块项目<b>零改动</b>。</para>
/// <para><b>代价（已知并接受）</b>：<c>DataDbContext</c> 的模型缓存键按「参与建模的配置<b>类型</b>清单」拼
/// （<c>ModuleSetModelCacheKeyFactory</c>）。汇总成一个适配器后，键里只有适配器类型 ⇒
/// 无法区分「装了哪几个业务模块」。CJPlug 恒全集注册，模型恒等，因此不构成问题；
/// 若将来要支持按模块裁剪，需改为逐配置注册。</para>
/// <para>⚠ 注册必须 <b>Singleton</b>：<c>SnapshotModelBuilder</c> 为 Singleton，注入
/// <c>IEnumerable&lt;IModuleDbConfig&gt;</c>，集合中任何 Scoped 元素都会让容器校验在启动期直接抛。</para>
/// </remarks>
public sealed class BusinessModuleDbConfigAdapter : CJCore.Modules.Data.IModuleDbConfig
{
    private readonly IReadOnlyList<IModuleBusinessDbConfig> _inner;

    public BusinessModuleDbConfigAdapter(IEnumerable<IModuleBusinessDbConfig> inner)
    {
        _inner = inner.ToList();
    }

    /// <summary>汇总到的业务模块配置数（供启动期自检打印：0 表示一个都没注册上）。</summary>
    public int Count => _inner.Count;

    /// <summary>汇总到的配置类型名（供启动期自检打印）。</summary>
    public IReadOnlyList<string> ModuleNames => _inner.Select(c => c.GetType().Name).ToList();

    public void AddDbSets(ModelBuilder modelBuilder)
    {
        foreach (var config in _inner)
        {
            config.AddDbSets(modelBuilder);
        }
    }

    public void ConfigEntities(ModelBuilder modelBuilder)
    {
        foreach (var config in _inner)
        {
            config.ConfigEntities(modelBuilder);
        }
    }
}
