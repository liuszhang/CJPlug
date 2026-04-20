using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Relation;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ToolActionSettingApi.DbContext
{
    public class TASModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Plug.Models.Plug.Plug>(entity=>entity.ToTable("Plugs"));
            modelBuilder.Entity<Plug.Models.Plug.PlugVariable>(entity=>entity.ToTable("PlugVariables"));
            modelBuilder.Entity<PlugToPlugAction>(entity => entity.ToTable("PlugToPlugActions"));



            Console.WriteLine("------>Success Add TAS Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Plug.Models.Plug.Plug>(entity =>
            entity.HasDiscriminator()
            .HasValue<Plug.Models.Plug.Plug>("Plug"));

            modelBuilder.Entity<Plug.Models.Plug.Plug>()
            .HasMany(a => a.PlugVariables)
            .WithOne(v => v.Plug)
            .HasForeignKey(v => v.PlugId)
            .OnDelete(DeleteBehavior.Cascade);

            Console.WriteLine("------>Success Add TAS Module Db EntityConfig");

        }
    }
}
