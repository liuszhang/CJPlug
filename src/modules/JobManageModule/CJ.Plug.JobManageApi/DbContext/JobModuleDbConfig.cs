using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Relation;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.JobManageApi.DbContext
{
    public class JobModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BaseJob>(entity => entity.ToTable("Jobs"));
            modelBuilder.Entity<ToolJob>();
            modelBuilder.Entity<ProcessJob>();


            Console.WriteLine("------>Success Add Job Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BaseJob>()
            .HasDiscriminator<string>(nameof(BaseJob.JobCategory)) // 区分不同子类的鉴别器字段
            .HasValue<ProcessJob>(JobCategoryEnum.ProcessJob.ToString())
            .HasValue<PlugJob>(JobCategoryEnum.PlugJob.ToString())
            .HasValue<ToolJob>(JobCategoryEnum.ToolJob.ToString()); // 注册所有子类

            Console.WriteLine("------>Success Add Job Module Db EntityConfig");

        }
    }
}
