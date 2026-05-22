using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CJ.Plug.Models.Services
{
    /// <summary>
    /// 种子数据执行器 - 收集并执行所有注册的 ISeedDataProvider
    /// </summary>
    public static class SeedDataRunner
    {
        /// <summary>
        /// 执行所有注册的种子数据提供者
        /// </summary>
        public static async Task RunAllAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var providers = serviceProvider.GetServices<ISeedDataProvider>().ToList();

            if (providers.Count == 0)
            {
                Console.WriteLine("[SeedData] 未注册任何种子数据提供者");
                return;
            }

            // 按 Order 排序
            var sortedProviders = providers.OrderBy(p => p.Order).ToList();

            Console.WriteLine($"[SeedData] 发现 {sortedProviders.Count} 个种子数据提供者");

            foreach (var provider in sortedProviders)
            {
                try
                {
                    Console.WriteLine($"[SeedData] 正在执行：{provider.Name} (Order={provider.Order})");
                    await provider.SeedAsync(serviceProvider, cancellationToken);
                    Console.WriteLine($"[SeedData] 完成：{provider.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SeedData] 失败：{provider.Name} - {ex.Message}");
                    Log.Error(ex, "[SeedData] 种子数据提供者 {ProviderName} 执行失败", provider.Name);
                    // 继续执行其他提供者，不因单个失败而中断
                }
            }

            Console.WriteLine("[SeedData] 所有种子数据提供者执行完成");
        }
    }
}
