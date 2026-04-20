using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.FileManageApi.DbContext
{
    public class FileModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileInformation>(entity => entity.ToTable("FileInformations"));

            Console.WriteLine("------>Success Add File Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            Console.WriteLine("------>Success Add File Module Db EntityConfig");

        }
    }
}
