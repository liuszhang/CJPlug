namespace CJ.Plug.LicenseModels
{
    /// <summary>
    /// 许可证验证结果
    /// </summary>
    public class LicenseValidationResult
    {
        /// <summary>是否有效</summary>
        public bool IsValid { get; set; }

        /// <summary>许可证信息（仅验证通过时有值）</summary>
        public LicenseInfo? License { get; set; }

        /// <summary>错误信息（验证失败时）</summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
