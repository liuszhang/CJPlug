public interface IPlugMarketApiClient
{
    Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default);
    Task<bool> DeleteMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default);
    Task<List<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default);
}
