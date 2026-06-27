using CJ.Plug.LicenseApi.Contracts;
using CJ.Plug.LicenseApi.DbContext;
using CJ.Plug.LicenseModels;
using Serilog;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CJ.Plug.LicenseApi.Services
{
    /// <summary>
    /// 许可证服务实现
    /// 启动时从数据库读取 License，缓存校验结果在内存
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _licenseFilePath;
        private LicenseValidationResult? _cachedResult;
        private readonly object _cacheLock = new();

        public LicenseService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
        public bool IsFeatureEnabled(LicenseFeature feature)
        {
            var current = GetCurrentLicense();
            return current.IsValid && current.License != null && current.License.Features.Contains(feature);
        }

        /// <inheritdoc/>
        public LicenseValidationResult ActivateLicense(string licenseKey)
        {
            // 1. 验签 + 机器指纹校验
            var validationResult = LicenseSigner.ValidateLicense(licenseKey);
            if (!validationResult.IsValid)
            {
                Log.Warning("许可证激活失败：{Error}", validationResult.ErrorMessage);
                return validationResult;
            }

            // 2. 持久化到数据库
            try
            {
                PersistToDatabase(validationResult.License!);
            }
            catch (Exception ex)
            {
                Log.Warning("许可证写入数据库失败，尝试文件存储：{Error}", ex.Message);
                PersistToFile(validationResult.License!);
            }

            // 3. 更新缓存
            lock (_cacheLock)
            {
                _cachedResult = validationResult;
            }

            Log.Information("许可证激活成功：{Licensee}，功能={Features}",
                validationResult.License!.Licensee,
                string.Join(",", validationResult.License.Features));

            return validationResult;
        }

        /// <inheritdoc/>
        public bool RevokeLicense()
        {
            try
            {
                // 从数据库标记为非活跃
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
        public string GenerateLicenseKey(List<LicenseFeature> features, DateTime? expiresAt, string licensee)
        {
            return LicenseSigner.SignLicense(features, expiresAt, licensee);
        }

        // ═══════════════════════════════════════════════════════
        // 持久化
        // ═══════════════════════════════════════════════════════

        private void LoadFromStorage()
        {
            try
            {
                // 优先从数据库加载
                var license = LoadFromDatabase();
                if (license != null)
                {
                    var result = LicenseSigner.ValidateLicense(license.LicenseKey);
                    // 即使验签失败也恢复缓存（可能是密钥更新等，让用户重新激活）
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
                    Features = JsonSerializer.Deserialize<List<LicenseFeature>>(entity.Features) ?? new(),
                    IssuedAt = entity.IssuedAt,
                    ExpiresAt = entity.ExpiresAt,
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
                Features = JsonSerializer.Serialize(license.Features),
                IssuedAt = license.IssuedAt,
                ExpiresAt = license.ExpiresAt,
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
    }
}
