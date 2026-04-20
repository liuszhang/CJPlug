using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.MCPTools;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CJ.Plug.MCPToolsManageApi.DbContext
{
    public class MCPToolsManageDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MCPTool>(entity => entity.ToTable("MCPTools"));


            Console.WriteLine("------>Success Add MCPTools Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {

            Console.WriteLine("------>Success Add Job Module Db EntityConfig");

        }
    }
}
