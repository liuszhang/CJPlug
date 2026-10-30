using CJCore.Framework.Abstractions;
using MudBlazor;

namespace PlugExecuteModule.Permissions
{
    /// <summary>
    /// 插件执行模块功能权限提供者。
    /// <para><b>接口归属（P5.5 第三批）</b>：必须实现 <see cref="CJCore.Framework.Abstractions.IFunctionPermissionProvider"/>，
    /// 而不是 CJPlug 自有的同名接口（<c>CJ.Plug.Models.Contracts</c>，已删除）。详见 JobManagePermissionProvider 类注释。</para>
    /// </summary>
    public class PlugExecutePermissionProvider : IFunctionPermissionProvider
    {
        /// <summary>
        /// 模块级权限键。必须与 <c>Module.ModuleId</c>（<c>plug-execute</c>）及
        /// <c>IModuleAccessDeclaration.AccessKey</c> 逐字一致。
        /// </summary>
        private const string ModuleKey = "plug-execute";

        // ⚠ P6.3 收口（2026-10-30）：必须与 IModule.Name 逐字一致（原值「流程执行」≠「插头执行」）。
        public string ModuleName => "插头执行";
        public string? ModuleIcon => Icons.Material.Filled.PlayArrow;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = ModuleKey + ":view",  Name = "查看执行记录", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "查看流程执行记录" },
                new() { Key = ModuleKey + ":start", Name = "启动执行",     ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "启动流程执行" },
                new() { Key = ModuleKey + ":stop",  Name = "停止执行",     ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "停止正在执行的流程" },
                new() { Key = ModuleKey + ":debug", Name = "调试执行",     ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "调试模式执行流程" },
            ];
        }
    }
}
