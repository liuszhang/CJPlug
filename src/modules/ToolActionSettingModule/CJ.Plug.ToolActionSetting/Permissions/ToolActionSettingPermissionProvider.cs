using CJ.Plug.Models.Contracts;
using MudBlazor;

namespace ToolActionSettingModule.Permissions
{
    /// <summary>
    /// 插件/工具管理模块功能权限提供者
    /// </summary>
    public class ToolActionSettingPermissionProvider : IFunctionPermissionProvider
    {
        public string ModuleName => "插件管理";
        public string? ModuleIcon => Icons.Material.Filled.Extension;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = "Plug.View", Name = "查看插件", ModuleName = ModuleName, Description = "查看插件列表和详情" },
                new() { Key = "Plug.Create", Name = "创建插件", ModuleName = ModuleName, Description = "创建新插件" },
                new() { Key = "Plug.Edit", Name = "编辑插件", ModuleName = ModuleName, Description = "编辑插件配置" },
                new() { Key = "Plug.Delete", Name = "删除插件", ModuleName = ModuleName, Description = "删除插件" },
                new() { Key = "Plug.Publish", Name = "发布插件", ModuleName = ModuleName, Description = "发布插件到市场" },
                new() { Key = "Station.View", Name = "查看工作站", ModuleName = ModuleName, Description = "查看工作站列表" },
                new() { Key = "Station.Manage", Name = "管理工作站", ModuleName = ModuleName, Description = "管理工作站配置" },
            ];
        }
    }
}
