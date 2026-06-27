using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace CJ.Plug.LicenseApi
{
    /// <summary>
    /// 机器指纹生成器
    /// </summary>
    public static class MachineFingerprint
    {
        /// <summary>
        /// 获取当前机器的唯一标识符
        /// 组合 MAC地址 + CPU序列号 + 主板序列号 的 SHA256 哈希（前16位十六进制）
        /// </summary>
        public static string GetMachineId()
        {
            var sb = new StringBuilder();

            try
            {
                // MAC 地址
                using var searcher = new ManagementObjectSearcher(
                    "SELECT MACAddress FROM Win32_NetworkAdapter WHERE NetConnectionStatus IS NOT NULL AND MACAddress IS NOT NULL");
                foreach (var obj in searcher.Get())
                {
                    var mac = obj["MACAddress"]?.ToString();
                    if (!string.IsNullOrEmpty(mac))
                    {
                        sb.Append(mac.Replace(":", "").ToUpperInvariant());
                        break;
                    }
                }
            }
            catch { /* 忽略 WMI 查询异常 */ }

            try
            {
                // CPU 序列号
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessorId FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    var cpuId = obj["ProcessorId"]?.ToString();
                    if (!string.IsNullOrEmpty(cpuId))
                    {
                        sb.Append(cpuId.Trim());
                        break;
                    }
                }
            }
            catch { }

            try
            {
                // 主板序列号
                using var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (var obj in searcher.Get())
                {
                    var boardSerial = obj["SerialNumber"]?.ToString();
                    if (!string.IsNullOrEmpty(boardSerial))
                    {
                        sb.Append(boardSerial.Trim());
                        break;
                    }
                }
            }
            catch { }

            // 如果 WMI 查询全部失败，回退到机器名 + 用户目录
            if (sb.Length == 0)
            {
                sb.Append(Environment.MachineName);
                sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash)[..16];
        }
    }
}
