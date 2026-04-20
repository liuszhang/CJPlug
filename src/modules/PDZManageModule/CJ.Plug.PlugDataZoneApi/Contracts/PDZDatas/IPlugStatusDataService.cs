using CJ.Plug.Models.Contracts;


namespace CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas
{
    public interface IPlugStatusDataService : IBaseRepositoryService<PlugStatusData, int>
    {
        // 新增 PlugStatusData 特有的方法（如按插头ID查询）
        Task<List<PlugStatusData>> GetByPlugDefinitionIdAsync(string plugDefinitionId, CancellationToken cancellationToken = default);
    }
}
