using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Relation;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.RelationApi.DbContext
{
    public class RelationModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommonRelation>(entity => entity.ToTable("Relations"));


            Console.WriteLine("------>Success Add Relation Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            Console.WriteLine("------>Success Add Relation Module Db EntityConfig");

        }
    }
}
