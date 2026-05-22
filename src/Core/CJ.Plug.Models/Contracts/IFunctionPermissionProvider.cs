using System.Collections.Generic;

namespace CJ.Plug.Models.Contracts
{
    /// <summary>
    /// 功能权限提供者接口 - 各模块实现此接口以声明自己提供的功能权限
    /// </summary>
    public interface IFunctionPermissionProvider
    {
        /// <summary>
        /// 模块名称（如：流程管理、插件管理）
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// 模块图标
        /// </summary>
        string? ModuleIcon { get; }

        /// <summary>
        /// 获取该模块提供的所有功能权限项
        /// </summary>
        List<FunctionPermissionItem> GetPermissions();
    }

    /// <summary>
    /// 功能权限项
    /// </summary>
    public class FunctionPermissionItem
    {
        /// <summary>
        /// 权限唯一标识（如：Process.Create, Process.Edit）
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 权限显示名称（如：创建流程）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 所属模块名称
        /// </summary>
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 权限分组（可选，用于在模块内进一步分组）
        /// </summary>
        public string? Group { get; set; }
    }
}
