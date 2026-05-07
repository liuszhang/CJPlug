using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.ApiClient
{
    public static class ApiClientServiceCollectionExtensions
    {
        /// <summary>
        /// 添加审计日志服务
        /// </summary>
        public static IServiceCollection AddAuditLogServices(this IServiceCollection services)
        {
            // AuditLogHelper 已经在 MainApiClient 内部创建，无需单独注册
            // 如果需要单独使用 IAuditApiClient，可以在这里注册
            return services;
        }
    }
}
