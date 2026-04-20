using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.PDZModels.DbContext
{
    public class PDZModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlugDataZone>(entity=>entity.ToTable("PlugDataZones"));
            modelBuilder.Entity<PlugData>(entity=>entity.ToTable("PlugDatas"));
            modelBuilder.Entity<PlugVariableData>(entity=>entity.ToTable("PlugVariableDatas"));
            modelBuilder.Entity<PlugStatusData>(entity=>entity.ToTable("PlugStatusDatas"));
            modelBuilder.Entity<ActionData>(entity=>entity.ToTable("ActionDatas"));
            modelBuilder.Entity<ActionVariableData>(entity=>entity.ToTable("ActionVariableDatas"));
            modelBuilder.Entity<FlowchartData>(entity=>entity.ToTable("FlowchartDatas"));
            modelBuilder.Entity<DataFlowData>(entity=>entity.ToTable("DataFlowDatas"));

            

            Console.WriteLine("------>Success Add PDZ Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlugDataZone>()
            .HasMany(a => a.PDZVariables)
            .WithOne(v => v.PlugDataZone)
            .HasForeignKey(v => v.PlugDataZoneId)
            .OnDelete(DeleteBehavior.Cascade);
            //将PDZVariable拆分为不同的类型，便于后续扩展和管理
            modelBuilder.Entity<PlugDataZone>()
                .HasMany(a => a.PlugDatas)
                .WithOne(v => v.PlugDataZone)
                .HasForeignKey(v => v.PlugDataZoneId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlugDataZone>()
                .HasMany(a => a.PlugVariableDatas)
                .WithOne(v => v.PlugDataZone)
                .HasForeignKey(v => v.PlugDataZoneId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlugDataZone>()
                .HasMany(a => a.PlugStatusDatas)
                .WithOne(v => v.PlugDataZone)
                .HasForeignKey(v => v.PlugDataZoneId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlugDataZone>()
                .HasMany(a => a.ActionDatas)
                .WithOne(v => v.PlugDataZone)
                .HasForeignKey(v => v.PlugDataZoneId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlugDataZone>()
                .HasMany(a => a.ActionVariableDatas)
                .WithOne(v => v.PlugDataZone)
                .HasForeignKey(v => v.PlugDataZoneId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlugDataZone>()
                .HasMany(a => a.FlowchartDatas)
                .WithOne(v => v.PlugDataZone)
                .HasForeignKey(v => v.PlugDataZoneId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlugDataZone>()
                .HasMany(a => a.DataFlowDatas)
                .WithOne(v => v.PlugDataZone)
                .HasForeignKey(v => v.PlugDataZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            Console.WriteLine("------>Success Add PDZ Module Db EntityConfig");

        }
    }
}
