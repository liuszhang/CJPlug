public interface IPlugMarketApiClient
{
    Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default);
    Task<bool> DeleteMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default);
    Task<List<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// 根据市场插头ID获取原始插头数据（含流程图和参数）
    /// </summary>
    Task<CJ.Plug.Models.Plug.Plug?> GetSourcePlugByMarketPlugIdAsync(int marketPlugId, CancellationToken cancellationToken = default);
}
