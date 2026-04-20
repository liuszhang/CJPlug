using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.PlugProcess;
using Microsoft.EntityFrameworkCore;
using CJ.Plug.Models.Relation;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Contracts;

//namespace CJ.Plug.Models.DbContexts;

public class MainDbContext : IdentityDbContext<User, UserRole,int>
{
    private readonly IEnumerable<IModuleDbConfig>? moduleDbConfigs;

    public MainDbContext(DbContextOptions<MainDbContext> options, IEnumerable<IModuleDbConfig>? moduleDbConfigs=null):base(options)
    {
        this.moduleDbConfigs = moduleDbConfigs;
    }


    //模型类定义放到各自模块中


    //public DbSet<Process> Processes { get; set; }
    //public DbSet<Plug> Plugs { get; set; }
    //public DbSet<PlugAction> PlugActions { get; set; }
    //public DbSet<MarketPlug> MarketPlugs { get; set; }
    //public DbSet<PlugVariable> PlugVariables { get; set; }
    //public DbSet<StationConfigTable> StationConfigTables { get; set; }
    //public DbSet<FileInformation> FileInformations { get; set; }

    //添加关系表，用于处理多对多关系
    //public DbSet<PlugToPlugAction> PlugToPlugActions { get; set; }

    //使用统一关系表关系所有的关系
    //public DbSet<CommonRelation> Relations { get; set; }

    //public DbSet<BaseJob> Jobs { get; set; }
    //public DbSet<ProcessJob> ProcessJobs { get; set; }
    //public DbSet<PlugJob> PlugJobs { get; set; }
    //public DbSet<ToolJob> ToolJobs { get; set; }


    //public DbSet<Tool> Tools { get; set; }
    //public DbSet<Station> Stations { get; set; }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ✅ 修复后：先调用基类配置扩展实体
        base.OnModelCreating(modelBuilder);

        //1.加载所有模块的DbSet
        foreach (var config in moduleDbConfigs)
        {
            config.AddDbSets(modelBuilder);
        }
        //2.加载所有模块的实体映射（包括TPH继承关系）
        foreach (var config in moduleDbConfigs)
        {
            config.ConfigEntities(modelBuilder);
        }


        // 配置 TPH 继承关系（基类 Plug，派生类 Process 等）
        //modelBuilder.Entity<Plug>()
        //    .HasDiscriminator<string>("Discriminator") // 指定鉴别器列名（默认就是 "Discriminator"）
        //    .HasValue<Plug>("Plug")
        //    .HasValue<Process>("Process")
        //    .HasValue<MarketPlug>("MarketPlug");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.Property(u => u.UserName).HasMaxLength(256).IsRequired(false);
        });
    }
}

