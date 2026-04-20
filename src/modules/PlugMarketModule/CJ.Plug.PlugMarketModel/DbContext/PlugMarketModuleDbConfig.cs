using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugProcess;
using Microsoft.EntityFrameworkCore;


    public class PlugMarketModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketPlug>();

            Console.WriteLine("------>Success Add PlugMarket Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MarketPlug>(entity =>
            {
                entity.HasBaseType<Plug>();
                entity.HasDiscriminator().HasValue<MarketPlug>("MarketPlug");
            });

            Console.WriteLine("------>Success Add PlugMarket Module Db EntityConfig");

        }
    }

