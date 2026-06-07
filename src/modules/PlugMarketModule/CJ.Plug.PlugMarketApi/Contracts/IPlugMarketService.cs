namespace CJ.Plug.PlugMarketApi.Contracts
{
    public interface IPlugMarketService
    {
        Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default);
        Task<IEnumerable<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default);
        Task<MarketPlug?> GetMarketPlugByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> DeleteMarketPlugAsync(int id, CancellationToken cancellationToken = default);
        Task<MarketPlug> UpdateMarketPlugAsync(int id, MarketPlug request, CancellationToken cancellationToken = default);
        /// <summary>
        /// 根据市场插头的 RootPlugID 获取原始插头数据（含流程图和参数）
        /// </summary>
        Task<CJ.Plug.Models.Plug.Plug?> GetSourcePlugByMarketPlugIdAsync(int marketPlugId, CancellationToken cancellationToken = default);
    }
}
