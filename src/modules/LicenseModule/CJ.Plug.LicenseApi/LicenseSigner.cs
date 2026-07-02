using CJ.Plug.LicenseModels;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CJ.Plug.LicenseApi
{
    /// <summary>
    /// AES 对称加密工具 — 负责许可证的生成与校验
    /// 授权码与设备/用户无关，AES 解密成功即视为认证通过
    /// </summary>
    public static class LicenseSigner
    {
        // ═══════════════════════════════════════════════════════
        // AES-256 密钥（硬编码，管理后台与客户端共享）
        // ═══════════════════════════════════════════════════════
        private const string AES_KEY_HEX = "672A4A5421F03B1EB4EBF3C3C5646CC0E3789841D5241762DA702997779E3446";

        /// <summary>
        /// 生成许可证密钥（仅管理后台/服务端使用）
        /// </summary>
        public static string SignLicense(string licensee, int validDays)
        {
            var license = new LicenseInfo
            {
                IssuedAt = DateTime.UtcNow,
                CodeExpiresAt = validDays == -1 ? null : DateTime.UtcNow.AddDays(validDays),
                Licensee = licensee
            };

            var payloadJson = JsonSerializer.Serialize(license);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(AES_KEY_HEX);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var cipherBytes = encryptor.TransformFinalBlock(payloadBytes, 0, payloadBytes.Length);

            // 格式：IV(16字节) + 密文 → Base64
            var combined = new byte[aes.IV.Length + cipherBytes.Length];
            Array.Copy(aes.IV, 0, combined, 0, aes.IV.Length);
            Array.Copy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// 校验许可证（客户端使用）
        /// </summary>
        public static LicenseValidationResult ValidateLicense(string licenseKey)
        {
            var result = new LicenseValidationResult();

            try
            {
                // 1. Base64 解码
                var combined = Convert.FromBase64String(licenseKey);

                // 2. 提取 IV（前16字节）和密文
                if (combined.Length < 16)
                {
                    result.ErrorMessage = "许可证格式无效";
                    return result;
                }

                var iv = new byte[16];
                Array.Copy(combined, 0, iv, 0, 16);

                var cipherBytes = new byte[combined.Length - 16];
                Array.Copy(combined, 16, cipherBytes, 0, cipherBytes.Length);

                // 3. AES 解密
                using var aes = Aes.Create();
                aes.Key = Convert.FromHexString(AES_KEY_HEX);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var payloadBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                // 4. JSON 反序列化
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                var license = JsonSerializer.Deserialize<LicenseInfo>(payloadJson);
                if (license == null)
                {
                    result.ErrorMessage = "许可证数据解析失败";
                    return result;
                }

                license.LicenseKey = licenseKey;

                // 5. 校验授权码自身过期时间
                if (license.CodeExpiresAt.HasValue && license.CodeExpiresAt.Value < DateTime.UtcNow)
                {
                    result.ErrorMessage = $"授权码已于 {license.CodeExpiresAt:yyyy-MM-dd} 过期";
                    return result;
                }

                result.IsValid = true;
                result.License = license;
            }
            catch (FormatException)
            {
                result.ErrorMessage = "许可证格式无效（Base64 解码失败）";
            }
            catch (CryptographicException)
            {
                result.ErrorMessage = "许可证解密失败，密钥无效或数据已损坏";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"许可证校验异常：{ex.Message}";
            }

            return result;
        }
    }
}
