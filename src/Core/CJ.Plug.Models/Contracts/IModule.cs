using CJCore.Framework.Abstractions;

namespace CJ.Plug.Models.Contracts
{
    /// <summary>
    /// CJPlug 的模块契约（2026-10-29《CJPlug 接入 CJCore》P1 / D2=A1）。
    /// <para>
    /// 原为独立的「只有 InitializeAsync」接口；现收敛为 CJCore 模块契约的**派生标记**，
    /// 这样任何 CJPlug 模块在类型系统层面必然也是一个 <see cref="CJCore.Framework.Abstractions.IModule"/>，
    /// 可以被框架壳（<c>FrameworkApp</c>）与模块目录（<c>ModuleAdmin</c>）发现。
    /// </para>
    /// <para>
    /// ⚠ <b>DI 注册口径</b>：MS DI 不会把派生接口的注册解析为基接口，因此各模块
    /// <c>AddScoped/Singleton&lt;IModule, X&gt;()</c> 已统一改为注册
    /// <c>CJCore.Framework.Abstractions.IModule</c>。本接口仅作为类型标记与兼容层保留，P2 之后随
    /// 旧壳一起清理。
    /// </para>
    /// </summary>
    public interface IModule : CJCore.Framework.Abstractions.IModule
    {
    }
}
