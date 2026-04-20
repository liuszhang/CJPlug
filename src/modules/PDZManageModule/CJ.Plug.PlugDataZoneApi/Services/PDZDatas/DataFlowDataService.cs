
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using CJ.Plug.Models.Services;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class DataFlowDataService : BaseRepositoryService<DataFlowData, int>, IDataFlowDataService
    {
        public DataFlowDataService(MainDbContext dbContext) : base(dbContext)
        {
        }
    }
}
