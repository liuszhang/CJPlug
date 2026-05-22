using CJ.Plug.AuditApi.Contracts;
using CJ.Plug.AuditApi.DbContext;
using CJ.Plug.AuditModels;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.AuditApi.Services
{
    public class AuditService : IAuditService
    {
        private readonly MainDbContext _dbContext;

        public AuditService(MainDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AuditLogDto> LogAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default)
        {
            var entity = MapToEntity(request);

            _dbContext.Set<AuditLogEntity>().Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Debug("审计日志已记录：{User} - {Module} - {Operation}", 
                request.UserName, request.Module, request.OperationType);

            return MapToDto(entity);
        }

        public async Task<PagedResult<AuditLogDto>> QueryAsync(AuditLogQueryRequest query, CancellationToken cancellationToken = default)
        {
            var queryable = _dbContext.Set<AuditLogEntity>().AsQueryable();

            // 应用筛选条件
            if (query.StartTime.HasValue)
                queryable = queryable.Where(x => x.OperationTime >= query.StartTime.Value);

            if (query.EndTime.HasValue)
                queryable = queryable.Where(x => x.OperationTime <= query.EndTime.Value);

            if (!string.IsNullOrWhiteSpace(query.UserName))
                queryable = queryable.Where(x => x.UserName.Contains(query.UserName));

            if (query.OperationType.HasValue)
                queryable = queryable.Where(x => x.OperationType == (int)query.OperationType.Value);

            if (query.Module.HasValue)
                queryable = queryable.Where(x => x.Module == (int)query.Module.Value);

            if (query.IsSuccess.HasValue)
                queryable = queryable.Where(x => x.IsSuccess == query.IsSuccess.Value);

            // 获取总数
            var totalCount = await queryable.CountAsync(cancellationToken);

            // 分页查询
            var items = await queryable
                .OrderByDescending(x => x.OperationTime)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => MapToDto(x))
                .ToListAsync(cancellationToken);

            return new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Set<AuditLogEntity>()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<int> CleanupAsync(int daysToKeep, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            
            var entitiesToDelete = await _dbContext.Set<AuditLogEntity>()
                .Where(x => x.OperationTime < cutoffDate)
                .ToListAsync(cancellationToken);

            if (entitiesToDelete.Count == 0)
                return 0;

            _dbContext.Set<AuditLogEntity>().RemoveRange(entitiesToDelete);
            var deletedCount = await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("已清理 {Count} 条 {Days} 天前的审计日志", deletedCount, daysToKeep);
            return deletedCount;
        }

        /// <summary>
        /// Map CreateAuditLogRequest to AuditLogEntity
        /// </summary>
        public static AuditLogEntity MapToEntity(CreateAuditLogRequest request)
        {
            return new AuditLogEntity
            {
                OperationTime = DateTime.Now,
                UserName = request.UserName,
                OperationType = (int)request.OperationType,
                Module = (int)request.Module,
                Description = request.Description,
                Detail = request.Detail,
                IpAddress = request.IpAddress,
                IsSuccess = request.IsSuccess,
                ErrorMessage = request.ErrorMessage
            };
        }

        public static AuditLogDto MapToDto(AuditLogEntity entity)
        {
            return new AuditLogDto
            {
                Id = entity.Id,
                OperationTime = entity.OperationTime,
                UserName = entity.UserName,
                OperationType = (AuditOperationType)entity.OperationType,
                Module = (AuditModule)entity.Module,
                Description = entity.Description,
                Detail = entity.Detail,
                IpAddress = entity.IpAddress,
                IsSuccess = entity.IsSuccess,
                ErrorMessage = entity.ErrorMessage
            };
        }
    }
}
