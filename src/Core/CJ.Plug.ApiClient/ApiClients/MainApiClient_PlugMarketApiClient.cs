//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
public partial class MainApiClient : IPlugMarketApiClient
{
    public async Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default)=>await PlugMarketApiClient.Value.CreateMarketPlugAsync(request, cancellationToken);
    public async Task<bool> DeleteMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default)=>await PlugMarketApiClient.Value.DeleteMarketPlugAsync(request, cancellationToken);
    public async Task<List<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default)=>await PlugMarketApiClient.Value.GetMarketPlugsAsync(cancellationToken);
}




