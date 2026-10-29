
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using CJ.Plug.Models.Services;
using CJCore.Modules.Data;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class DataFlowDataService : BaseRepositoryService<DataFlowData, int>, IDataFlowDataService
    {
        public DataFlowDataService(DataDbContext dbContext) : base(dbContext)
        {
        }
    }
}
