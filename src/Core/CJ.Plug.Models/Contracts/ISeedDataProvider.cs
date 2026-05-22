namespace CJ.Plug.Models.Contracts
{
    /// <summary>
    /// 种子数据提供者接口 - 各模块可选实现，在应用启动时执行初始化
    /// </summary>
    public interface ISeedDataProvider
    {
        /// <summary>
        /// 提供者名称（用于日志输出）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 执行顺序（数字越小越先执行，默认100）
        /// </summary>
        int Order => 100;

        /// <summary>
        /// 执行种子数据初始化
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
    }
}
