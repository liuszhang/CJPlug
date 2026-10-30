using CJCore.Framework.Abstractions;
using MudBlazor;

namespace PDZManageModule.Permissions
{
    /// <summary>
    /// 数据空间管理模块功能权限提供者。
    /// <para><b>接口归属（P5.5 第三批）</b>：必须实现 <see cref="CJCore.Framework.Abstractions.IFunctionPermissionProvider"/>，
    /// 而不是 CJPlug 自有的同名接口（<c>CJ.Plug.Models.Contracts</c>，已删除）。详见 JobManagePermissionProvider 类注释。</para>
    /// </summary>
    public class PDZManagePermissionProvider : IFunctionPermissionProvider
    {
        /// <summary>
        /// 模块级权限键。必须与 <c>Module.ModuleId</c>（<c>plug-pdz</c>）及
        /// <c>IModuleAccessDeclaration.AccessKey</c> 逐字一致。
        /// </summary>
        private const string ModuleKey = "plug-pdz";

        // ⚠ P6.3 收口（2026-10-30）：必须与 IModule.Name 逐字一致 —— DefaultModuleCatalog.BuildFeatures
        //   经 IsSameModule(声明名, 模块名) 反查归属，差一个字功能点就在矩阵里挂不上父节点。
        public string ModuleName => "插头数据区(PDZ)";
        public string? ModuleIcon => Icons.Material.Filled.Storage;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = ModuleKey + ":view",   Name = "查看数据空间", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "查看数据空间列表" },
                new() { Key = ModuleKey + ":create", Name = "创建数据空间", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "创建新数据空间" },
                new() { Key = ModuleKey + ":edit",   Name = "编辑数据空间", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "编辑数据空间配置" },
                new() { Key = ModuleKey + ":delete", Name = "删除数据空间", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "删除数据空间" },
                new() { Key = ModuleKey + ":export", Name = "导出数据",     ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "导出数据空间数据" },
            ];
        }
    }
}
