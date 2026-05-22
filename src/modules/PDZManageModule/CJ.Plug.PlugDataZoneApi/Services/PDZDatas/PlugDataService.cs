using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class PlugDataService : BaseRepositoryService<PlugData, int>, IPlugDataService
    {
        private MainDbContext DbContext { get; set; }
        public PlugDataService(MainDbContext dbContext) : base(dbContext)
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
