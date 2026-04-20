using System.Net.Http.Json;

public class PlugMarketApiClient : BaseApiClient, IPlugMarketApiClient
{
    public PlugMarketApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
    {
    }

    public async Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/plug/createMarketPlug", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MarketPlug>(cancellationToken: cancellationToken);
    }

    public async Task<List<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/plug/getMarketPlugs", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MarketPlug>>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.DeleteAsync($"/api/plug/deleteMarketPlug/{request.Id}", cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            //var newRelation = new CommonRelation()
            //{
            //    RelationCategory = RelationCategory.UserToMarketPlug.ToString(),
            //    RoleBId = request.Id
            //};
            //await relationApiClient.DeleteRealationAsync(newRelation, cancellationToken);
            return true;
        }
        else
        {
            // 处理错误情况
            var errorMessage = await result.Content.ReadAsStringAsync();
            Console.WriteLine($"Error delete marketplug: {errorMessage}");
            return false;
        }
    }
}
