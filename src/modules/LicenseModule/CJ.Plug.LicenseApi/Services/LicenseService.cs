using CJ.Plug.LicenseApi.Contracts;
using CJ.Plug.LicenseApi.DbContext;
using CJ.Plug.LicenseModels;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace CJ.Plug.LicenseApi.Services
{
    /// <summary>
    /// 许可证服务实现
    /// 启动时从数据库读取 License，缓存校验结果在内存
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly HttpClient _httpClient;
        private readonly string _licenseFilePath;
        private LicenseValidationResult? _cachedResult;
        private readonly object _cacheLock = new();

        // ═══════════════════════════════════════════════════════
        // 码支付配置
        // ═══════════════════════════════════════════════════════
        private readonly string _mazhifuAppId;
        private readonly string _mazhifuAppSecret;
        private readonly string _mazhifuBaseUrl;
        private readonly string _notifyUrl;

        // ═══════════════════════════════════════════════════════
        // 升级订单表（内存，重启后清空）
        // ═══════════════════════════════════════════════════════
        private readonly ConcurrentDictionary<string, UpgradeOrder> _upgradeOrders = new();

        public LicenseService(IServiceProvider serviceProvider, IConfiguration configuration, HttpClient httpClient)
        {
            _serviceProvider = serviceProvider;
            _httpClient = httpClient;

            _mazhifuAppId = configuration["Mazhifu:AppId"] ?? string.Empty;
            _mazhifuAppSecret = configuration["Mazhifu:AppSecret"] ?? string.Empty;
            _mazhifuBaseUrl = configuration["Mazhifu:ApiBaseUrl"] ?? "https://api.mazhifupay.com";
            _notifyUrl = configuration["Mazhifu:NotifyUrl"] ?? string.Empty;

            _licenseFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CJ.Plug",
                "license.dat");

            // 启动时加载
            LoadFromStorage();
        }

        /// <inheritdoc/>
        public LicenseValidationResult GetCurrentLicense()
        {
            lock (_cacheLock)
            {
                return _cachedResult ?? new LicenseValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "未激活许可证"
                };
            }
        }

        /// <inheritdoc/>
        public LicenseValidationResult ActivateLicense(string licenseKey)
        {
            var validationResult = LicenseSigner.ValidateLicense(licenseKey);
            if (!validationResult.IsValid)
            {
                Log.Warning("许可证激活失败：{Error}", validationResult.ErrorMessage);
                return validationResult;
            }

            // 持久化到数据库
            try
            {
                PersistToDatabase(validationResult.License!);
            }
            catch (Exception ex)
            {
                Log.Warning("许可证写入数据库失败，尝试文件存储：{Error}", ex.Message);
                PersistToFile(validationResult.License!);
            }

            // 更新缓存
            lock (_cacheLock)
            {
                _cachedResult = validationResult;
            }

            Log.Information("许可证激活成功：{Licensee}",
                validationResult.License!.Licensee);

            return validationResult;
        }

        /// <inheritdoc/>
        public bool RevokeLicense()
        {
            try
            {
                RemoveFromDatabase();
            }
            catch (Exception ex)
            {
                Log.Warning("从数据库撤销许可证失败：{Error}", ex.Message);
            }

            // 删除本地文件
            try
            {
                if (File.Exists(_licenseFilePath))
                    File.Delete(_licenseFilePath);
            }
            catch { }

            lock (_cacheLock)
            {
                _cachedResult = new LicenseValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "许可证已撤销"
                };
            }

            Log.Information("许可证已撤销");
            return true;
        }

        /// <inheritdoc/>
        public string GenerateLicenseKey(string licensee, int validDays)
        {
            return LicenseSigner.SignLicense(licensee, validDays);
        }

        // ═══════════════════════════════════════════════════════
        // 持久化
        // ═══════════════════════════════════════════════════════

        private void LoadFromStorage()
        {
            try
            {
                var license = LoadFromDatabase();
                if (license != null)
                {
                    var result = LicenseSigner.ValidateLicense(license.LicenseKey);
                    lock (_cacheLock)
                    {
                        _cachedResult = result;
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("从数据库加载许可证失败：{Error}", ex.Message);
            }

            // 回退：从文件加载
            try
            {
                if (File.Exists(_licenseFilePath))
                {
                    var licenseKey = File.ReadAllText(_licenseFilePath).Trim();
                    if (!string.IsNullOrEmpty(licenseKey))
                    {
                        var result = LicenseSigner.ValidateLicense(licenseKey);
                        lock (_cacheLock)
                        {
                            _cachedResult = result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("从文件加载许可证失败：{Error}", ex.Message);
            }
        }

        private LicenseInfo? LoadFromDatabase()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
                var entity = dbContext.Set<LicenseEntity>()
                    .FirstOrDefault(l => l.IsActive);

                if (entity == null) return null;

                return new LicenseInfo
                {
                    LicenseKey = entity.LicenseKey,
                    IssuedAt = entity.IssuedAt,
                    Licensee = entity.Licensee
                };
            }
            catch
            {
                return null;
            }
        }

        private void PersistToDatabase(LicenseInfo license)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

            // 将之前的所有许可证标记为非活跃
            var existing = dbContext.Set<LicenseEntity>().Where(l => l.IsActive);
            foreach (var e in existing)
                e.IsActive = false;

            // 写入新许可证
            var entity = new LicenseEntity
            {
                LicenseKey = license.LicenseKey,
                IssuedAt = license.IssuedAt,
                ExpiresAt = null,
                Licensee = license.Licensee,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Set<LicenseEntity>().Add(entity);
            dbContext.SaveChanges();
        }

        private void PersistToFile(LicenseInfo license)
        {
            var dir = Path.GetDirectoryName(_licenseFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_licenseFilePath, license.LicenseKey);
        }

        private void RemoveFromDatabase()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var existing = dbContext.Set<LicenseEntity>().Where(l => l.IsActive);
            foreach (var e in existing)
                e.IsActive = false;
            dbContext.SaveChanges();
        }

        // ═══════════════════════════════════════════════════════
        // 升级 / 支付
        // ═══════════════════════════════════════════════════════

        /// <inheritdoc/>
        public UpgradeOrderResponse CreateUpgradeOrder()
        {
            var orderId = Guid.NewGuid().ToString("N")[..12];
            var amount = 6.00m;

            var response = new UpgradeOrderResponse
            {
                OrderId = orderId,
                Status = "pending",
                Amount = amount
            };

            // 如果未配置码支付，回退到本地占位二维码
            if (string.IsNullOrEmpty(_mazhifuAppId) || string.IsNullOrEmpty(_mazhifuAppSecret))
            {
                Log.Warning("未配置码支付 AppId/AppSecret，使用占位二维码");
                var order = new UpgradeOrder { OrderId = orderId, Status = "pending", Amount = amount, CreatedAt = DateTime.UtcNow };
                _upgradeOrders[orderId] = order;
                return response;
            }

            // 调用码支付创建订单
            try
            {
                var qrCodeUrl = CreateMazhifuOrder(orderId, amount);
                response.QrCodeUrl = qrCodeUrl;

                var order = new UpgradeOrder
                {
                    OrderId = orderId,
                    Status = "pending",
                    Amount = amount,
                    CreatedAt = DateTime.UtcNow,
                    QrCodeUrl = qrCodeUrl
                };
                _upgradeOrders[orderId] = order;

                Log.Information("升级订单已创建（码支付）：{OrderId}，二维码：{QrCodeUrl}", orderId, qrCodeUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "调用码支付创建订单失败，回退到本地模式：{OrderId}", orderId);
                var order = new UpgradeOrder { OrderId = orderId, Status = "pending", Amount = amount, CreatedAt = DateTime.UtcNow };
                _upgradeOrders[orderId] = order;
            }

            return response;
        }

        /// <inheritdoc/>
        public UpgradeStatusResponse GetUpgradeOrderStatus(string orderId)
        {
            if (!_upgradeOrders.TryGetValue(orderId, out var order))
            {
                return new UpgradeStatusResponse
                {
                    OrderId = orderId,
                    Status = "expired",
                    Message = "订单不存在或已过期"
                };
            }

            return new UpgradeStatusResponse
            {
                OrderId = orderId,
                Status = order.Status,
                Message = order.Status switch
                {
                    "pending" => "等待扫码支付",
                    "paid" => "支付成功，正在激活...",
                    "activated" => "激活完成",
                    _ => "未知状态"
                },
                LicenseStatus = order.Status == "activated"
                    ? BuildLicenseStatusResponse()
                    : null
            };
        }

        /// <inheritdoc/>
        public UpgradeStatusResponse ConfirmUpgradePayment(string orderId)
        {
            if (!_upgradeOrders.TryGetValue(orderId, out var order))
            {
                return new UpgradeStatusResponse
                {
                    OrderId = orderId,
                    Status = "expired",
                    Message = "订单不存在或已过期"
                };
            }

            if (order.Status != "pending")
            {
                return new UpgradeStatusResponse
                {
                    OrderId = orderId,
                    Status = order.Status,
                    Message = order.Status == "activated" ? "已完成激活" : "订单状态异常"
                };
            }

            // 标记已支付
            order.Status = "paid";
            Log.Information("升级订单 {OrderId} 支付确认", orderId);

            // 自动生成并激活许可证（永久有效）
            try
            {
                var licenseKey = GenerateLicenseKey("UpgradeUser", -1);
                var result = ActivateLicense(licenseKey);

                if (result.IsValid)
                {
                    order.Status = "activated";
                    Log.Information("升级订单 {OrderId} 自动激活成功", orderId);
                }
                else
                {
                    Log.Warning("升级订单 {OrderId} 自动激活失败：{Error}", orderId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "升级订单 {OrderId} 自动激活异常", orderId);
            }

            return new UpgradeStatusResponse
            {
                OrderId = orderId,
                Status = order.Status,
                Message = order.Status == "activated" ? "激活完成" : "激活失败",
                LicenseStatus = order.Status == "activated"
                    ? BuildLicenseStatusResponse()
                    : null
            };
        }

        /// <inheritdoc/>
        public UpgradeStatusResponse? VerifyPaymentCallback(string orderId, string money, string type, string sign, string? tradeNo)
        {
            if (string.IsNullOrEmpty(_mazhifuAppSecret))
            {
                Log.Warning("码支付未配置 AppSecret，无法验证回调");
                return null;
            }

            // 验证签名：MD5(app_id + out_trade_no + money + type + app_secret)
            var expectedSign = GenerateMazhifuSign(orderId, money, type);
            if (!string.Equals(sign, expectedSign, StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("码支付回调验签失败：orderId={OrderId}, sign={Sign}, expected={Expected}",
                    orderId, sign, expectedSign);
                return null;
            }

            Log.Information("码支付回调验签通过：orderId={OrderId}, tradeNo={TradeNo}", orderId, tradeNo);

            // 更新订单的码支付交易号
            if (!string.IsNullOrEmpty(tradeNo) && _upgradeOrders.TryGetValue(orderId, out var order))
            {
                order.TradeNo = tradeNo;
            }

            // 调用已有确认逻辑
            return ConfirmUpgradePayment(orderId);
        }

        // ═══════════════════════════════════════════════════════
        // 码支付 API 调用
        // ═══════════════════════════════════════════════════════

        private string GenerateMazhifuSign(string orderId, string money, string type)
        {
            var raw = $"{_mazhifuAppId}{orderId}{money}{type}{_mazhifuAppSecret}";
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private string CreateMazhifuOrder(string orderId, decimal amount)
        {
            var money = amount.ToString("F2");
            var type = "wxpay";
            var sign = GenerateMazhifuSign(orderId, money, type);

            var payload = new
            {
                app_id = _mazhifuAppId,
                out_trade_no = orderId,
                money,
                type,
                notify_url = _notifyUrl,
                sign
            };

            var url = $"{_mazhifuBaseUrl.TrimEnd('/')}/api/v1/order/create";
            var response = _httpClient.PostAsJsonAsync(url, payload).GetAwaiter().GetResult();

            response.EnsureSuccessStatusCode();
            var result = response.Content.ReadFromJsonAsync<MazhifuOrderResponse>().GetAwaiter().GetResult();

            if (result == null || string.IsNullOrEmpty(result.QrcodeUrl))
            {
                throw new InvalidOperationException("码支付返回的二维码 URL 为空");
            }

            if (result.Code != 0)
            {
                throw new InvalidOperationException($"码支付创建订单失败：{result.Message}");
            }

            return result.QrcodeUrl;
        }

        private LicenseStatusResponse BuildLicenseStatusResponse()
        {
            var current = GetCurrentLicense();
            var response = new LicenseStatusResponse
            {
                IsActivated = current.IsValid,
                Message = current.IsValid ? "已激活" : current.ErrorMessage
            };

            if (current.IsValid && current.License != null)
            {
                response.Licensee = current.License.Licensee;
                response.IssuedAt = current.License.IssuedAt;
                response.CodeExpiresAt = current.License.CodeExpiresAt;
                response.IsExpired = current.License.CodeExpiresAt.HasValue
                    && current.License.CodeExpiresAt.Value < DateTime.UtcNow;
            }

            return response;
        }
    }

    /// <summary>
    /// 码支付创建订单响应
    /// </summary>
    internal class MazhifuOrderResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public string QrcodeUrl { get; set; } = string.Empty;
        public string OutTradeNo { get; set; } = string.Empty;
    }

    /// <summary>
    /// 内存中的升级订单记录
    /// </summary>
    internal class UpgradeOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string QrCodeUrl { get; set; } = string.Empty;
        public string? TradeNo { get; set; }
    }
}
