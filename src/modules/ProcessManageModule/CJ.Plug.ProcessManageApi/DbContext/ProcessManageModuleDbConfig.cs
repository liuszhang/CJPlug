using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugProcess;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ProcessManageApi.DbContext
{
    public class ProcessManageModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Process>();

            Console.WriteLine("------>Success Add ProcessManage Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Process>(entity =>
            {
                entity.HasBaseType<Plug.Models.Plug.Plug>();
                entity.HasDiscriminator().HasValue<Process>("Process");
            });

            Console.WriteLine("------>Success Add ProcessManage Module Db EntityConfig");

        }
    }
}
