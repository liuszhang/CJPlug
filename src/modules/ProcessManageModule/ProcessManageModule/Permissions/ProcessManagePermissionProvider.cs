using CJ.Plug.Models.Contracts;
using MudBlazor;

namespace ProcessManageModule.Permissions
{
    /// <summary>
    /// 流程管理模块功能权限提供者
    /// </summary>
    public class ProcessManagePermissionProvider : IFunctionPermissionProvider
    {
        public string ModuleName => "流程管理";
        public string? ModuleIcon => Icons.Material.Filled.AccountTree;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = "Process.View", Name = "查看流程", ModuleName = ModuleName, Description = "查看流程列表和详情" },
                new() { Key = "Process.Create", Name = "创建流程", ModuleName = ModuleName, Description = "创建新流程" },
                new() { Key = "Process.Edit", Name = "编辑流程", ModuleName = ModuleName, Description = "编辑流程信息" },
                new() { Key = "Process.Delete", Name = "删除流程", ModuleName = ModuleName, Description = "删除流程" },
                new() { Key = "Process.Export", Name = "导出流程", ModuleName = ModuleName, Description = "导出流程数据" },
                new() { Key = "Process.Import", Name = "导入流程", ModuleName = ModuleName, Description = "导入流程数据" },
            ];
        }
    }
}
