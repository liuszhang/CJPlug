using CJCore.Framework.Abstractions;
using MudBlazor;

namespace ProcessManageModule.Permissions
{
    /// <summary>
    /// 流程管理模块功能权限提供者。
    /// <para><b>接口归属（P5.5 第三批）</b>：必须实现 <see cref="CJCore.Framework.Abstractions.IFunctionPermissionProvider"/>，
    /// 而不是 CJPlug 自有的同名接口（<c>CJ.Plug.Models.Contracts</c>，已删除）。详见 JobManagePermissionProvider 类注释。</para>
    /// </summary>
    public class ProcessManagePermissionProvider : IFunctionPermissionProvider
    {
        /// <summary>
        /// 模块级权限键。必须与 <c>Module.ModuleId</c>（<c>plug-process-manage</c>，注意带 <c>-manage</c> 后缀，
        /// 与 JobManage 同构）及 <c>IModuleAccessDeclaration.AccessKey</c> 逐字一致。
        /// </summary>
        private const string ModuleKey = "plug-process-manage";

        public string ModuleName => "流程管理";
        public string? ModuleIcon => Icons.Material.Filled.AccountTree;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = ModuleKey + ":view",   Name = "查看流程", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "查看流程列表和详情" },
                new() { Key = ModuleKey + ":create", Name = "创建流程", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "创建新流程" },
                new() { Key = ModuleKey + ":edit",   Name = "编辑流程", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "编辑流程信息" },
                new() { Key = ModuleKey + ":delete", Name = "删除流程", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "删除流程" },
                new() { Key = ModuleKey + ":export", Name = "导出流程", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "导出流程数据" },
                new() { Key = ModuleKey + ":import", Name = "导入流程", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "导入流程数据" },
            ];
        }
    }
}
