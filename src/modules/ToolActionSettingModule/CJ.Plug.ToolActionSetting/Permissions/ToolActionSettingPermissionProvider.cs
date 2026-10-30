using CJCore.Framework.Abstractions;
using MudBlazor;

namespace ToolActionSettingModule.Permissions
{
    /// <summary>
    /// 插件/工具管理模块功能权限提供者。
    /// <para><b>接口归属（P5.5 第三批）</b>：必须实现 <see cref="CJCore.Framework.Abstractions.IFunctionPermissionProvider"/>，
    /// 而不是 CJPlug 自有的同名接口（<c>CJ.Plug.Models.Contracts</c>，已删除）。详见 JobManagePermissionProvider 类注释。</para>
    /// <para><b>与同模块 IModuleAccessDeclaration 的关系</b>：两者<b>不是同一体系</b>、也不是替代关系 ——
    /// <c>IModuleAccessDeclaration</c>（AccessKey=<c>plug-tool-action</c>）声明「模块级角色白名单」，喂给鉴权判定链；
    /// 本类声明的是「模块内功能点」，喂给权限矩阵当按钮行。两者靠 <c>ParentKey</c> 关联。</para>
    /// </summary>
    public class ToolActionSettingPermissionProvider : IFunctionPermissionProvider
    {
        /// <summary>
        /// 模块级权限键。必须与 <c>Module.ModuleId</c>（<c>plug-tool-action</c>）及
        /// <c>IModuleAccessDeclaration.AccessKey</c> 逐字一致。
        /// </summary>
        private const string ModuleKey = "plug-tool-action";

        // ⚠ P6.3 收口（2026-10-30）：原值「插件管理」与 IModule.Name「工具动作配置」不一致 ⇒
        //   该模块的 7 个功能点被 BuildFeatures 过滤掉、模块 features 恒为 0。改为模块真名。
        public string ModuleName => "工具动作配置";
        public string? ModuleIcon => Icons.Material.Filled.Extension;

        public List<FunctionPermissionItem> GetPermissions()
        {
            return
            [
                new() { Key = ModuleKey + ":view",            Name = "查看插件",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "查看插件列表和详情" },
                new() { Key = ModuleKey + ":create",          Name = "创建插件",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "创建新插件" },
                new() { Key = ModuleKey + ":edit",            Name = "编辑插件",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "编辑插件配置" },
                new() { Key = ModuleKey + ":delete",          Name = "删除插件",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "删除插件" },
                new() { Key = ModuleKey + ":publish",         Name = "发布插件",   ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "发布插件到市场" },

                // ⚠ 以下两项（工位查看/管理）历史上声明在本模块，但工位管理其实是独立模块（StationManageModule）。
                //   本方案按 D2 决议「暂留原处」，仅随接口归口一并改键名；归属是否迁出单独议。
                new() { Key = ModuleKey + ":station-view",    Name = "查看工作站", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "查看工作站列表" },
                new() { Key = ModuleKey + ":station-manage",  Name = "管理工作站", ModuleName = ModuleName, Group = ModuleName, ParentKey = ModuleKey, Description = "管理工作站配置" },
            ];
        }
    }
}
