using CJ.Plug.Models.Contracts;
using CJ.Plug.AuditApi.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.AuditApi.DbContext
{
    public class AuditModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLogEntity>(entity => 
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.OperationTime);
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.OperationType);
                entity.HasIndex(e => e.Module);
            });
            
            Console.WriteLine("------>Success Add Audit Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            Console.WriteLine("------>Success Add Audit Module Db EntityConfig");
        }
    }
}
