namespace CJ.Plug.UserManageModels
{
    /// <summary>
    /// 通用数据状态枚举
    /// </summary>
    public enum DataStatus
    {
        /// <summary>
        /// 授权中 - 已提交授权请求，等待审批
        /// </summary>
        Authorizing = -1,

        /// <summary>
        /// 禁用
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// 启用
        /// </summary>
        Active = 1
    }

    /// <summary>
    /// 状态显示辅助类
    /// </summary>
    public static class DataStatusHelper
    {
        public static string GetDisplayName(this DataStatus status) => status switch
        {
            DataStatus.Authorizing => "授权中",
            DataStatus.Disabled => "禁用",
            DataStatus.Active => "启用",
            _ => "未知"
        };

        public static string GetColor(this DataStatus status) => status switch
        {
            DataStatus.Authorizing => "Warning",
            DataStatus.Disabled => "Default",
            DataStatus.Active => "Success",
            _ => "Default"
        };
    }
}
