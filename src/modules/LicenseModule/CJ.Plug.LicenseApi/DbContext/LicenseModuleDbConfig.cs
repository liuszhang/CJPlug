using CJ.Plug.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.LicenseApi.DbContext
{
    /// <summary>
    /// 许可证模块数据库配置
    /// </summary>
    public class LicenseModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LicenseEntity>(entity => entity.ToTable("Licenses"));
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LicenseEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LicenseKey).IsRequired();
                entity.Property(e => e.Licensee).HasMaxLength(256);
            });
        }
    }

    /// <summary>
    /// 许可证数据库实体
    /// </summary>
    public class LicenseEntity
    {
        public int Id { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Licensee { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
