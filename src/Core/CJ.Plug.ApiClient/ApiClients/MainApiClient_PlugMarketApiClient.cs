using CJ.Plug.AuditModels;

public partial class MainApiClient : IPlugMarketApiClient
{
    public async Task<MarketPlug> CreateMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default)
    {
        var result = await PlugMarketApiClient.Value.CreateMarketPlugAsync(request, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Create, $"创建市场插件: {request.Name}");
        return result;
    }

    public async Task<bool> DeleteMarketPlugAsync(MarketPlug request, CancellationToken cancellationToken = default)
    {
        var result = await PlugMarketApiClient.Value.DeleteMarketPlugAsync(request, cancellationToken);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Delete, $"删除市场插件: {request.Name}");
        return result;
    }

    public async Task<List<MarketPlug>> GetMarketPlugsAsync(CancellationToken cancellationToken = default)
    {
        var result = await PlugMarketApiClient.Value.GetMarketPlugsAsync(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, "查询所有市场插件");
        return result;
    }
}
