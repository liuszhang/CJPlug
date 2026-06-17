using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.StationManageApi.DbContext
{
    public class StationManageDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Station>(entity=>entity.ToTable("Stations"));
            modelBuilder.Entity<Tool>(entity=>entity.ToTable("Tools"));
            modelBuilder.Entity<StationConfigTable>(entity=>entity.ToTable("StationConfigTables"));

            

            Console.WriteLine("------>Success Add StationManage Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            

            Console.WriteLine("------>Success Add StationManage Module Db EntityConfig");

        }
    }
}
