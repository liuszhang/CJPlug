using CJ.Plug.LicenseModels;

namespace CJ.Plug.LicenseApi.Contracts
{
    /// <summary>
    /// 许可证服务接口
    /// </summary>
    public interface ILicenseService
    {
        /// <summary>获取当前激活的许可证</summary>
        LicenseValidationResult GetCurrentLicense();

        /// <summary>检查指定功能是否已激活</summary>
        bool IsFeatureEnabled(LicenseFeature feature);

        /// <summary>激活许可证并持久化</summary>
        LicenseValidationResult ActivateLicense(string licenseKey);

        /// <summary>撤销当前许可证</summary>
        bool RevokeLicense();

        /// <summary>生成许可证密钥（管理后台）</summary>
        string GenerateLicenseKey(List<LicenseFeature> features, DateTime? expiresAt, string licensee);
    }
}
