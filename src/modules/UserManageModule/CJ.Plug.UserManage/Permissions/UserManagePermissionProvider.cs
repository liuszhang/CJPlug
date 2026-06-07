using CJ.Plug.Models.Contracts;
using MudBlazor;

namespace UserManageModule.Permissions
{
    /// <summary>
    /// 用户管理模块功能权限提供者
    /// </summary>
    public class UserManagePermissionProvider : IFunctionPermissionProvider
    {
        public string ModuleName => "用户管理";
        public string? ModuleIcon => Icons.Material.Filled.ManageAccounts;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = "User.View", Name = "查看用户", ModuleName = ModuleName, Description = "查看用户列表和详情" },
                new() { Key = "User.Create", Name = "创建用户", ModuleName = ModuleName, Description = "创建新用户" },
                new() { Key = "User.Edit", Name = "编辑用户", ModuleName = ModuleName, Description = "编辑用户信息" },
                new() { Key = "User.Delete", Name = "删除用户", ModuleName = ModuleName, Description = "删除用户" },
                new() { Key = "User.EnableDisable", Name = "启用/禁用用户", ModuleName = ModuleName, Description = "启用或禁用用户账号" },
                new() { Key = "User.LockUnlock", Name = "锁定/解锁用户", ModuleName = ModuleName, Description = "锁定或解锁用户账号" },
                new() { Key = "Role.View", Name = "查看角色", ModuleName = ModuleName, Description = "查看角色列表" },
                new() { Key = "Role.Configure", Name = "配置角色", ModuleName = ModuleName, Description = "配置角色权限和成员" },
                new() { Key = "Department.View", Name = "查看部门", ModuleName = ModuleName, Description = "查看部门列表" },
                new() { Key = "Department.Manage", Name = "管理部门", ModuleName = ModuleName, Description = "创建、编辑、删除部门" },
            ];
        }
    }
}
