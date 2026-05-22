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
            modelBuilder.Entity<UserGroup>(entity => entity.ToTable("UserGroups"));
            modelBuilder.Entity<UserGroupMember>(entity => entity.ToTable("UserGroupMembers"));
            modelBuilder.Entity<Department>(entity => entity.ToTable("Departments"));
            modelBuilder.Entity<RoleFunctionPermission>(entity => entity.ToTable("RoleFunctionPermissions"));
            modelBuilder.Entity<RoleDataPermission>(entity => entity.ToTable("RoleDataPermissions"));


            Console.WriteLine("------>Success Add User Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            // UserGroup 主键和基本配置
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Id).ValueGeneratedOnAdd();
            });

            // UserGroup 和 User 的多对多关系配置
            // UserGroup -> UserGroupMember (one-to-many, 反向导航：UserGroupMember.UserGroup)
            //modelBuilder.Entity<UserGroup>()
            //    .HasMany(g => g.GroupMembers)
            //    .WithOne(m => m.UserGroup)
            //    .HasForeignKey(m => m.UserGroupId)
            //    .OnDelete(DeleteBehavior.Cascade);

            // User -> UserGroupMember (one-to-many, 反向导航：UserGroupMember.User)
            modelBuilder.Entity<User>()
                .HasMany(u => u.GroupMembers).WithOne(m => m.User).HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserGroupMember 主键（独立实体，复合主键：UserId + RoleId）
            modelBuilder.Entity<UserGroupMember>(entity =>
            {
                entity.HasKey(m => new { m.UserId, m.UserGroupId });
            });

            Console.WriteLine("------>Success Add User Module Db EntityConfig");
        }
    }
}
