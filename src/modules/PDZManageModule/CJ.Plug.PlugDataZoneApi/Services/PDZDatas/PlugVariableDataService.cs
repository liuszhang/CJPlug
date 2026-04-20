
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class PlugVariableDataService : BaseRepositoryService<PlugVariableData, int>, IPlugVariableDataService
    {
        public PlugVariableDataService(MainDbContext dbContext) : base(dbContext)
        {
        }
    }
}
