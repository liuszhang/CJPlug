using CJ.Plug.Models.Contracts;
using MudBlazor;

namespace PDZManageModule.Permissions
{
    /// <summary>
    /// 数据空间管理模块功能权限提供者
    /// </summary>
    public class PDZManagePermissionProvider : IFunctionPermissionProvider
    {
        public string ModuleName => "数据空间";
        public string? ModuleIcon => Icons.Material.Filled.Storage;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = "PDZ.View", Name = "查看数据空间", ModuleName = ModuleName, Description = "查看数据空间列表" },
                new() { Key = "PDZ.Create", Name = "创建数据空间", ModuleName = ModuleName, Description = "创建新数据空间" },
                new() { Key = "PDZ.Edit", Name = "编辑数据空间", ModuleName = ModuleName, Description = "编辑数据空间配置" },
                new() { Key = "PDZ.Delete", Name = "删除数据空间", ModuleName = ModuleName, Description = "删除数据空间" },
                new() { Key = "PDZ.Export", Name = "导出数据", ModuleName = ModuleName, Description = "导出数据空间数据" },
            ];
        }
    }
}
