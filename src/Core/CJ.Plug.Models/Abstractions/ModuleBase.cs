using CJ.Plug.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Abstractions
{
    /// <summary>
    /// 模块基类（2026-10-29《CJPlug 接入 CJCore》P1 / D2=A1 换契约）。
    /// <para>
    /// 由「自带 IModule 的最小基类」改为**继承 CJCore 的 <c>ModuleBase</c>**：
    /// 26 个模块子类因此一行都不用改，同时天然获得 CJCore 的模块元数据能力
    /// （ModuleId / Version / DependsOn / IsMandatory / Group / Order）。
    /// </para>
    /// <para>
    /// 仍实现本项目的 <see cref="IModule"/>（已收敛为 CJCore 契约的派生标记），
    /// 因此 <c>AddScoped&lt;IModule, X&gt;</c> 这类既有注册在过渡期内继续可用。
    /// </para>
    /// <para>
    /// <b>Name 的临时口径</b>：CJCore 的 <c>ModuleBase.Name</c> 是 abstract，这里用「命名空间末段」
    /// 作为过渡值（如 <c>CJ.Plug.FileManage</c> → <c>FileManage</c>），避免 29 个子类同时改动。
    /// P6 补模块元数据时，各模块再按 <c>plug-&lt;模块&gt;</c> 规范覆写 <c>Name</c> / <c>ModuleId</c> / <c>Group</c>。
    /// </para>
    /// </summary>
    public abstract class ModuleBase : CJCore.Framework.Abstractions.ModuleBase, IModule
    {
        /// <summary>
        /// 过渡期的模块显示名：命名空间末段（缺省回退类型名）。P6 各模块应覆写为业务中文名。
        /// </summary>
        public override string Name
        {
            get
            {
                var ns = GetType().Namespace;
                if (string.IsNullOrWhiteSpace(ns))
                {
                    return GetType().Name;
                }

                var segment = ns.Split('.').LastOrDefault(s => !string.IsNullOrWhiteSpace(s));
                return string.IsNullOrWhiteSpace(segment) ? GetType().Name : segment;
            }
        }
    }
}
