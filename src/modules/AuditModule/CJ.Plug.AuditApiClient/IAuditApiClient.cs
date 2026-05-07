using CJ.Plug.AuditModels;

namespace CJ.Plug.AuditApiClient
{
    public interface IAuditApiClient
    {
        /// <summary>
        /// 记录审计日志
        /// </summary>
        Task<AuditLogDto> LogAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 查询审计日志
        /// </summary>
        Task<PagedResult<AuditLogDto>> QueryAsync(AuditLogQueryRequest query, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取审计日志详情
        /// </summary>
        Task<AuditLogDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 清理指定天数之前的日志
        /// </summary>
        Task<int> CleanupAsync(int daysToKeep, CancellationToken cancellationToken = default);
    }
}
