using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;
using CJCore.Modules.Data;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class FlowchartDataService : BaseRepositoryService<FlowchartData, int>, IFlowchartDataService
    {
        public FlowchartDataService(DataDbContext dbContext) : base(dbContext)
        {
        }
    }
}
