
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;
using CJCore.Modules.Data;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class PlugVariableDataService : BaseRepositoryService<PlugVariableData, int>, IPlugVariableDataService
    {
        public PlugVariableDataService(DataDbContext dbContext) : base(dbContext)
        {
        }
    }
}
