using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;

using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ApiServer.Services.PDZDatas
{
    public class PlugStatusDataService : BaseRepositoryService<PlugStatusData, int>, IPlugStatusDataService
    {
        private MainDbContext PDZDbContext { get; set; }
        public PlugStatusDataService(MainDbContext dbContext) : base(dbContext)
        {
            PDZDbContext = dbContext;
        }

        // 实现专用方法
        public async Task<List<PlugStatusData>> GetByPlugDefinitionIdAsync(
            string plugDefinitionId,
            CancellationToken cancellationToken = default)
        {
            return await PDZDbContext.Set<PlugStatusData>()
                .Where(p => p.PlugDefinitionId == plugDefinitionId)
                .ToListAsync(cancellationToken);
        }

        // 如需重写通用方法（如自定义更新逻辑），可覆盖 BaseService 中的实现
        //public override async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        //{
        //    // 自定义更新逻辑（如校验 PlugDefinitionId 不为空）
        //    if (string.IsNullOrEmpty(entity.PlugDefinitionId))
        //    {
        //        throw new ArgumentException("插头ID不能为空");
        //    }
        //    return await base.UpdateAsync(entity, cancellationToken);
        //}
    }
}
