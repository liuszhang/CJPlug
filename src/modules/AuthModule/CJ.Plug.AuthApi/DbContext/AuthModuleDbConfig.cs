using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.AuthApi.DbContext
{
    public class AuthModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthRequestEntity>(entity => entity.ToTable("AuthRequests"));
            Console.WriteLine("------>Success Add Auth Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            Console.WriteLine("------>Success Add Auth Module Db EntityConfig");
        }
    }

    /// <summary>
    /// 授权请求数据库实体
    /// </summary>
    public class AuthRequestEntity
    {
        public int Id { get; set; }
        public int OperationType { get; set; }
        public string TargetDescription { get; set; } = string.Empty;
        public string OperationData { get; set; } = string.Empty;
        /// <summary>
        /// 关联的目标数据ID（创建操作时记录）
        /// </summary>
        public int TargetId { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int Status { get; set; } = 0;
        public string? Remark { get; set; }
    }
}
