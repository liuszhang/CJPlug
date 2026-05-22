using CJ.Plug.Models.Contracts;
using MudBlazor;

namespace JobManageModule.Permissions
{
    /// <summary>
    /// 作业管理模块功能权限提供者
    /// </summary>
    public class JobManagePermissionProvider : IFunctionPermissionProvider
    {
        public string ModuleName => "作业管理";
        public string? ModuleIcon => Icons.Material.Filled.Work;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = "Job.View", Name = "查看作业", ModuleName = ModuleName, Description = "查看作业列表和详情" },
                new() { Key = "Job.Create", Name = "创建作业", ModuleName = ModuleName, Description = "创建新作业" },
                new() { Key = "Job.Edit", Name = "编辑作业", ModuleName = ModuleName, Description = "编辑作业配置" },
                new() { Key = "Job.Delete", Name = "删除作业", ModuleName = ModuleName, Description = "删除作业" },
                new() { Key = "Job.Execute", Name = "执行作业", ModuleName = ModuleName, Description = "手动执行作业" },
            ];
        }
    }
}
