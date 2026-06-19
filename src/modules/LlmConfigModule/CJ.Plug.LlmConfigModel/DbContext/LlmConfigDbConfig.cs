using CJ.Plug.LlmConfigModel.Models;
using CJ.Plug.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.LlmConfigModel.DbContext;

public class LlmConfigDbConfig : IModuleDbConfig
{
    public void AddDbSets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LlmProvider>(entity => entity.ToTable("LlmProviders"));
        modelBuilder.Entity<LlmModelConfig>(entity => entity.ToTable("LlmModelConfigs"));
        modelBuilder.Entity<McpServerConfig>(entity => entity.ToTable("McpServerConfigs"));

        Console.WriteLine("------>Success Add LlmConfig Module DbSet Config");
    }

    public void ConfigEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LlmProvider>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.ModelConfigs)
                  .WithOne()
                  .HasForeignKey(m => m.LlmProviderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LlmModelConfig>(entity =>
        {
            entity.HasIndex(e => new { e.LlmProviderId, e.ModelName }).IsUnique();
            entity.HasIndex(e => e.ModelType);
        });

        Console.WriteLine("------>Success Add LlmConfig Module Db EntityConfig");
    }
}
