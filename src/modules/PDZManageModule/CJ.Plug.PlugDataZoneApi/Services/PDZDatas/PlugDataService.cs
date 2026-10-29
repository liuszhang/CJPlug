using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;
using CJCore.Modules.Data;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class PlugDataService : BaseRepositoryService<PlugData, int>, IPlugDataService
    {
        private DataDbContext DbContext { get; set; }
        public PlugDataService(DataDbContext dbContext) : base(dbContext)
        {
            DbContext = dbContext;
        }

        public async Task<bool> DeleteByDefinitionId(string plugDefinitionId, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<PlugData>()
                .Where(p => p.PlugDefinitionId == plugDefinitionId)
                .ExecuteDeleteAsync(cancellationToken);
            return true;
        }
    }
}
