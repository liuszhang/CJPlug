using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.UserManageApi.Services
{
    /// <summary>
    /// 用户管理模块种子数据提供者 - 创建默认角色、部门和管理员用户
    /// </summary>
    public class UserManageSeedDataProvider : ISeedDataProvider
    {
        public string Name => "用户管理模块种子数据";
        public int Order => 10; // 较早执行，因为其他模块可能依赖角色和部门

        public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<UserRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // 1. 创建系统角色
            await SeedRolesAsync(roleManager, cancellationToken);

            // 2. 创建默认部门
            await SeedDepartmentsAsync(serviceProvider, cancellationToken);

            // 3. 创建默认用户组
            await SeedGroupsAsync(serviceProvider, cancellationToken);

            // 4. 创建管理员用户
            await SeedAdminUserAsync(userManager, roleManager, cancellationToken);
        }

        private static async Task SeedDepartmentsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var dbContext = serviceProvider.GetRequiredService<MainDbContext>();

            var seedDepartments = new (string Name, string Code)[]
            {
                ("开发部", "DEV"),
            };

            foreach (var (name, code) in seedDepartments)
            {
                var existing = await dbContext.Set<Department>()
                    .FirstOrDefaultAsync(d => d.Name == name, cancellationToken);

                if (existing == null)
                {
                    var dept = new Department
                    {
                        Name = name,
                        Code = code,
                        Status = 1, // 启用
                        Creator = "system"
                    };

                    dbContext.Set<Department>().Add(dept);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    Log.Information("[SeedData] 成功创建部门：{Name}", name);
                    Console.WriteLine($"[SeedData] 成功创建部门：{name}");
                }
            }
        }

        private static async Task SeedGroupsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var dbContext = serviceProvider.GetRequiredService<MainDbContext>();

            var seedGroups = new (string Name, string Description)[]
            {
                ("专家组", "技术专家团队，拥有高级功能权限"),
            };

            foreach (var (name, description) in seedGroups)
            {
                var existing = await dbContext.UserGroups
                    .FirstOrDefaultAsync(g => g.Name == name, cancellationToken);

                if (existing == null)
                {
                    var group = new UserGroup
                    {
                        Name = name,
                        Description = description,
                        Status = 1, // 启用
                        Creator = "system"
                    };

                    dbContext.UserGroups.Add(group);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    Log.Information("[SeedData] 成功创建用户组：{Name}", name);
                    Console.WriteLine($"[SeedData] 成功创建用户组：{name}");
                }
            }
        }

        private static async Task SeedRolesAsync(RoleManager<UserRole> roleManager, CancellationToken cancellationToken)
        {
            var systemRoles = new (string Name, string Description, string RoleType)[]
            {
                ("系统管理员", "系统超级管理员，拥有所有权限", "系统角色"),
                ("授权管理员", "负责审批各类授权请求", "系统角色"),
                ("审计管理员", "负责查看和管理审计日志", "系统角色"),
                ("普通用户", "默认用户角色，拥有基本功能权限", "业务角色"),
            };

            foreach (var (name, description, roleType) in systemRoles)
            {
                var existingRole = await roleManager.FindByNameAsync(name);
                if (existingRole == null)
                {
                    var role = new UserRole
                    {
                        Name = name,
                        Description = description,
                        RoleType = roleType,
                        Status = 1, // 启用
                        IsSystem = true // 系统内置，不可删除和编辑
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        Log.Information("[SeedData] 成功创建系统角色：{RoleName}", name);
                        Console.WriteLine($"[SeedData] 成功创建系统角色：{name}");
                    }
                    else
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        Log.Warning("[SeedData] 创建系统角色 {RoleName} 失败：{Errors}", name, errors);
                        Console.WriteLine($"[SeedData] 创建系统角色 {name} 失败：{errors}");
                    }
                }
                else
                {
                    // 确保现有系统角色的 IsSystem 标记正确
                    if (!existingRole.IsSystem)
                    {
                        existingRole.IsSystem = true;
                        await roleManager.UpdateAsync(existingRole);
                        Log.Information("[SeedData] 更新系统角色 {RoleName} 的 IsSystem 标记", name);
                    }
                }
            }
        }

        private static async Task SeedAdminUserAsync(
            UserManager<User> userManager,
            RoleManager<UserRole> roleManager,
            CancellationToken cancellationToken)
        {
            const string adminUserName = "admin";
            const string adminPassword = "123456";

            var existingUser = await userManager.FindByNameAsync(adminUserName);
            if (existingUser == null)
            {
                var user = new User
                {
                    UserName = adminUserName,
                    Email = "admin@cjplug.com",
                    FirstName = "系统",
                    LastName = "管理员",
                    Status = 1, // 启用
                    IsSystem = true, // 系统内置，不可删除和编辑
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, adminPassword);
                if (result.Succeeded)
                {
                    Log.Information("[SeedData] 成功创建管理员用户：{UserName}", adminUserName);
                    Console.WriteLine($"[SeedData] 成功创建管理员用户：{adminUserName}");

                    // 分配系统管理员角色
                    var addToRoleResult = await userManager.AddToRoleAsync(user, "系统管理员");
                    if (addToRoleResult.Succeeded)
                    {
                        Log.Information("[SeedData] 成功为管理员用户分配系统管理员角色");
                        Console.WriteLine("[SeedData] 成功为管理员用户分配系统管理员角色");
                    }
                    else
                    {
                        var errors = string.Join("; ", addToRoleResult.Errors.Select(e => e.Description));
                        Log.Warning("[SeedData] 为管理员用户分配角色失败：{Errors}", errors);
                    }
                }
                else
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    Log.Warning("[SeedData] 创建管理员用户失败：{Errors}", errors);
                    Console.WriteLine($"[SeedData] 创建管理员用户失败：{errors}");
                }
            }
            else
            {
                // 确保现有管理员用户的 IsSystem 标记正确
                if (!existingUser.IsSystem)
                {
                    existingUser.IsSystem = true;
                    await userManager.UpdateAsync(existingUser);
                    Log.Information("[SeedData] 更新管理员用户 {UserName} 的 IsSystem 标记", adminUserName);
                }
            }
        }
    }
}
