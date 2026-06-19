using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ToolResourceApi.DbContext
{
    public class ToolResourceDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Station>(entity=>entity.ToTable("Stations"));
            modelBuilder.Entity<Tool>(entity=>entity.ToTable("Tools"));
            modelBuilder.Entity<StationConfigTable>(entity=>entity.ToTable("StationConfigTables"));

            

            Console.WriteLine("------>Success Add ToolResource Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            

            Console.WriteLine("------>Success Add ToolResource Module Db EntityConfig");

        }
    }
}
