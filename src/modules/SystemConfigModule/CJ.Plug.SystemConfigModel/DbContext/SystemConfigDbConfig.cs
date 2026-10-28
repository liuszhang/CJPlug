using CJ.Plug.Models.Contracts;
using CJ.Plug.SystemConfigModel.Models;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.SystemConfigModel.DbContext;

public class SystemConfigDbConfig : IModuleDbConfig
{
    public void AddDbSets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemConfigItem>(entity => entity.ToTable("SystemConfigItems"));
        Console.WriteLine("------>Success Add SystemConfig Module DbSet Config");
    }

    public void ConfigEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemConfigItem>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });
        Console.WriteLine("------>Success Add SystemConfig Module Db EntityConfig");
    }
}
