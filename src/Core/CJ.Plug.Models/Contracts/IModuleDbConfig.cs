using CJCore.Modules.Data;

namespace CJ.Plug.Models.Contracts
{
    /// <summary>
    /// CJPlug 的「<b>身份侧</b>」数据库配置契约（P4 / 2026-10-29）。
    /// </summary>
    /// <remarks>
    /// <para>它是 CJCore <see cref="CJCore.Modules.Data.IModuleDbConfig"/> 的<b>派生标记</b>：
    /// 方法签名完全一致，<b>实现方零改动</b>（只需把基接口换成这个标记）。</para>
    /// <para>之所以要分两个标记而不是直接用 CJCore 的接口，是因为两个上下文要各取所需：</para>
    /// <list type="bullet">
    ///   <item>本标记 ⇒ 只有 <c>MainDbContext</c> 注入的集合能看到（当前仅 <c>UserModuleDbConfig</c>：
    ///         Users / UserRoles / UserGroups / UserGroupMembers / Departments / RoleFunctionPermissions / RoleDataPermissions ——
    ///         这些表被 ASP.NET Identity 与权限服务直接使用，必须留在 <c>MainDbContext</c> 模型里，等 P5 换 ThreeRoleAdmin 才搬）。</item>
    ///   <item><see cref="IModuleBusinessDbConfig"/> ⇒ 由适配器汇总后喂给 CJCore 的 <c>DataDbContext</c>（业务表）。</item>
    /// </list>
    /// <para>⚠ 为什么需要显式适配器、而不是直接把业务配置注册成 CJCore 的接口：MS DI <b>不会</b>把
    /// 派生接口的注册解析成基接口（P1 已踩过一次：<c>IModule</c> 换契约后 28 处注册仍需逐个改）。
    /// 这里用「一个适配器对象汇总 N 个业务配置」换取「18 个模块项目都不必新增 CJCore 引用」。</para>
    /// </remarks>
    public interface IModuleDbConfig : CJCore.Modules.Data.IModuleDbConfig
    {
    }

    /// <summary>
    /// CJPlug 的「<b>业务侧</b>」数据库配置契约（P4 / 2026-10-29）。
    /// </summary>
    /// <remarks>
    /// <para>18 个业务模块的 DbConfig 实现本标记 ⇒ 由 <c>BusinessModuleDbConfigAdapter</c> 汇总成
    /// 一个 CJCore <see cref="CJCore.Modules.Data.IModuleDbConfig"/> 注册进 DI，
    /// 从而被 <c>DataDbContext.OnModelCreating</c> 与自动迁移引擎的 <c>SnapshotModelBuilder</c> 收集。</para>
    /// <para>⚠ 注册必须 <b>Singleton</b>：<c>SnapshotModelBuilder</c> 是 Singleton 且注入
    /// <c>IEnumerable&lt;IModuleDbConfig&gt;</c>，集合里出现任一 Scoped 元素会在 Development 的容器校验期直接炸。</para>
    /// </remarks>
    public interface IModuleBusinessDbConfig : CJCore.Modules.Data.IModuleDbConfig
    {
    }
}
