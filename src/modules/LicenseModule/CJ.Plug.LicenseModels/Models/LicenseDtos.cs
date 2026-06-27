namespace CJ.Plug.LicenseModels
{
    /// <summary>
    /// 激活许可证请求
    /// </summary>
    public class ActivateLicenseRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// 许可证状态响应
    /// </summary>
    public class LicenseStatusResponse
    {
        public bool IsActivated { get; set; }
        public string Licensee { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime? CodeExpiresAt { get; set; }
        public bool IsExpired { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 生成许可证请求（管理后台）
    /// </summary>
    public class GenerateLicenseRequest
    {
        public string Licensee { get; set; } = string.Empty;
        /// <summary>有效天数，-1 表示永久</summary>
        public int ValidDays { get; set; } = 90;
    }

    /// <summary>
    /// 生成许可证响应
    /// </summary>
    public class GenerateLicenseResponse
    {
        public string LicenseKey { get; set; } = string.Empty;
        public LicenseInfo License { get; set; } = new();
    }
}
