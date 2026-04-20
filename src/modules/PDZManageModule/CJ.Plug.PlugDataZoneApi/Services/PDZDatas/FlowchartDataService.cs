using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class FlowchartDataService : BaseRepositoryService<FlowchartData, int>, IFlowchartDataService
    {
        public FlowchartDataService(MainDbContext dbContext) : base(dbContext)
        {
        }
    }
}
