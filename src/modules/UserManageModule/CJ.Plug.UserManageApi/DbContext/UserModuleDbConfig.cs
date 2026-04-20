using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Relation;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageModels;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.UserManageApi.DbContext
{
    public class UserModuleDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity => entity.ToTable("Users"));
            modelBuilder.Entity<UserRole>(entity => entity.ToTable("UserRoles"));
            modelBuilder.Entity<Department>(entity => entity.ToTable("Departments"));


            Console.WriteLine("------>Success Add User Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {

            Console.WriteLine("------>Success Add User Module Db EntityConfig");

        }
    }
}
