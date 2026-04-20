using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.StationAndToolApi.DbContext
{
    public class StationAndToolDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Station>(entity=>entity.ToTable("Stations"));
            modelBuilder.Entity<Tool>(entity=>entity.ToTable("Tools"));
            modelBuilder.Entity<StationConfigTable>(entity=>entity.ToTable("StationConfigTables"));

            

            Console.WriteLine("------>Success Add StationAndTool Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            

            Console.WriteLine("------>Success Add StationAndTool Module Db EntityConfig");

        }
    }
}
