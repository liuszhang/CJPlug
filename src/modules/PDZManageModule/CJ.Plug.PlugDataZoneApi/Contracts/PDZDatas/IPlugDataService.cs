using CJ.Plug.Models.Contracts;


namespace CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas
{
    public interface IPlugDataService : IBaseRepositoryService<PlugData, int>
    {
        Task<bool> DeleteByDefinitionId(string plugDefinitionId, CancellationToken cancellationToken = default);
    }
}
