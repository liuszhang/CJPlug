using CJCore.Framework.Abstractions;
using MudBlazor;

namespace JobManageModule.Permissions
{
    /// <summary>
    /// 作业管理模块功能权限提供者。
    /// <para><b>接口归属（P5.5 第三批）</b>：必须实现 <see cref="CJCore.Framework.Abstractions.IFunctionPermissionProvider"/>，
    /// 而不是 CJPlug 自有的同名接口（<c>CJ.Plug.Models.Contracts</c>，已删除）。两侧同名不同版：
    /// CJCore 版多出 <c>ParentKey</c>/<c>Kind</c>/<c>Category</c>，且只有它被
    /// <c>PermissionFunctionsService</c> 注入 —— 注册错版本会导致功能点在权限矩阵里<b>静默消失</b>（无异常无告警）。</para>
    /// </summary>
    public class JobManagePermissionProvider : IFunctionPermissionProvider
    {
        /// <summary>
        /// 模块级权限键。必须与 <c>Module.ModuleId</c> 及 <c>IModuleAccessDeclaration.AccessKey</c> 逐字一致：
        /// 前者决定模块启停的持久化键，后者决定 <c>DefaultModuleCatalog.BuildFeatures</c> 的归属反查。
        /// 三者分叉的后果是功能点在矩阵里成为孤儿（平铺在根，不挂在模块行下）。
        /// </summary>
        private const string ModuleKey = "plug-job-manage";

        public string ModuleName => "作业管理";
        public string? ModuleIcon => Icons.Material.Filled.Work;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = ModuleKey + ":view",    Name = "查看作业",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "查看作业列表和详情" },
                new() { Key = ModuleKey + ":create",  Name = "创建作业",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "创建新作业" },
                new() { Key = ModuleKey + ":edit",    Name = "编辑作业",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "编辑作业配置" },
                new() { Key = ModuleKey + ":delete",  Name = "删除作业",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "删除作业" },
                new() { Key = ModuleKey + ":execute", Name = "执行作业",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "手动执行作业" },
            ];
        }
    }
}
