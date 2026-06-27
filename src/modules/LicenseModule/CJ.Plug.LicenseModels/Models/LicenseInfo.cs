namespace CJ.Plug.LicenseModels
{
    /// <summary>
    /// 许可证信息
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>加密签名的许可证密钥</summary>
        public string LicenseKey { get; set; } = string.Empty;

        /// <summary>已授权功能列表</summary>
        public List<LicenseFeature> Features { get; set; } = new();

        /// <summary>签发时间</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>到期时间（null = 永久）</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>授权码自身的过期时间（null = 不限）</summary>
        public DateTime? CodeExpiresAt { get; set; }

        /// <summary>被许可人/公司</summary>
        public string Licensee { get; set; } = string.Empty;

        /// <summary>是否已过期</summary>
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

        /// <summary>是否为永久许可证</summary>
        public bool IsPerpetual => !ExpiresAt.HasValue;
    }
}
