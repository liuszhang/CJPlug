namespace CJ.Plug.PlugMarketApi.Contracts
{
    public interface IPlugMarketService
    {
        Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default);
        Task<IEnumerable<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default);
        Task<MarketPlug?> GetMarketPlugByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> DeleteMarketPlugAsync(int id, CancellationToken cancellationToken = default);
        Task<MarketPlug> UpdateMarketPlugAsync(int id, MarketPlug request, CancellationToken cancellationToken = default);
    }
}
