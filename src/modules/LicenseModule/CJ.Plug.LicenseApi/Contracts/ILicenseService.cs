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

        /// <summary>激活许可证并持久化</summary>
        LicenseValidationResult ActivateLicense(string licenseKey);

        /// <summary>撤销当前许可证</summary>
        bool RevokeLicense();

        /// <summary>生成许可证密钥（管理后台）</summary>
        string GenerateLicenseKey(string licensee, int validDays);

        // ═══════════════════════════════════════════════════════
        // 升级 / 支付
        // ═══════════════════════════════════════════════════════

        /// <summary>创建升级订单</summary>
        UpgradeOrderResponse CreateUpgradeOrder();

        /// <summary>查询升级订单状态</summary>
        UpgradeStatusResponse GetUpgradeOrderStatus(string orderId);

        /// <summary>确认支付（模拟支付回调）</summary>
        UpgradeStatusResponse ConfirmUpgradePayment(string orderId);

        /// <summary>码支付回调验签并确认支付</summary>
        /// <returns>null 表示验签失败，非 null 表示处理结果</returns>
        UpgradeStatusResponse? VerifyPaymentCallback(string orderId, string money, string type, string sign, string? tradeNo);
    }
}
