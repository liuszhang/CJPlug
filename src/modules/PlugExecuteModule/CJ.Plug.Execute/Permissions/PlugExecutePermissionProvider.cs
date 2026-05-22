using CJ.Plug.Models.Contracts;
using MudBlazor;

namespace PlugExecuteModule.Permissions
{
    /// <summary>
    /// 插件执行模块功能权限提供者
    /// </summary>
    public class PlugExecutePermissionProvider : IFunctionPermissionProvider
    {
        public string ModuleName => "流程执行";
        public string? ModuleIcon => Icons.Material.Filled.PlayArrow;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = "Execute.View", Name = "查看执行记录", ModuleName = ModuleName, Description = "查看流程执行记录" },
                new() { Key = "Execute.Start", Name = "启动执行", ModuleName = ModuleName, Description = "启动流程执行" },
                new() { Key = "Execute.Stop", Name = "停止执行", ModuleName = ModuleName, Description = "停止正在执行的流程" },
                new() { Key = "Execute.Debug", Name = "调试执行", ModuleName = ModuleName, Description = "调试模式执行流程" },
            ];
        }
    }
}
