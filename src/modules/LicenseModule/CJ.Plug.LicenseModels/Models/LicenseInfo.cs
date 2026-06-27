namespace CJ.Plug.LicenseModels
{
    /// <summary>
    /// 许可证信息（简化版：只做纯粹的加密认证，不关联功能）
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>加密签名的许可证密钥</summary>
        public string LicenseKey { get; set; } = string.Empty;

        /// <summary>签发时间</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>授权码自身的过期时间（null = 不限）</summary>
        public DateTime? CodeExpiresAt { get; set; }

        /// <summary>被许可人/公司</summary>
        public string Licensee { get; set; } = string.Empty;
    }
}
