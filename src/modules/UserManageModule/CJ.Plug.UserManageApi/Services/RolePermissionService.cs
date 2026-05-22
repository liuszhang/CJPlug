using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.UserManageApi.Services
{
    public class RolePermissionService : IRolePermissionService
    {
        private readonly MainDbContext _dbContext;
        private readonly RoleManager<UserRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IEnumerable<IFunctionPermissionProvider> _permissionProviders;

        public RolePermissionService(
            MainDbContext dbContext,
            RoleManager<UserRole> roleManager,
            UserManager<User> userManager,
            IEnumerable<IFunctionPermissionProvider> permissionProviders)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
            _userManager = userManager;
            _permissionProviders = permissionProviders;
        }

        public Task<List<FunctionPermissionDefinitionDto>> GetAllPermissionDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            var definitions = new List<FunctionPermissionDefinitionDto>();

            foreach (var provider in _permissionProviders)
            {
                var permissions = provider.GetPermissions();
                definitions.Add(new FunctionPermissionDefinitionDto
                {
                    ModuleName = provider.ModuleName,
                    ModuleIcon = provider.ModuleIcon,
                    Permissions = permissions.Select(p => new FunctionPermissionItemDto
                    {
                        Key = p.Key,
                        Name = p.Name,
                        ModuleName = p.ModuleName,
                        Description = p.Description,
                        Group = p.Group
                    }).ToList()
                });
            }

            return Task.FromResult(definitions);
        }

        public async Task<RoleConfigDto?> GetRoleConfigAsync(int roleId, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null) return null;

            var memberIds = await GetRoleMemberIdsAsync(roleId, cancellationToken);
            var funcPermissions = await GetRoleFunctionPermissionsAsync(roleId, cancellationToken);
            var dataPermissions = await GetRoleDataPermissionsAsync(roleId, cancellationToken);

            return new RoleConfigDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                RoleDescription = role.Description,
                RoleType = role.RoleType,
                MemberUserIds = memberIds,
                FunctionPermissions = funcPermissions,
                DataPermissions = dataPermissions
            };
        }

        public async Task<List<RoleFunctionPermissionDto>> GetRoleFunctionPermissionsAsync(int roleId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<RoleFunctionPermission>()
                .Where(p => p.RoleId == roleId)
                .Select(p => new RoleFunctionPermissionDto
                {
                    Id = p.Id,
                    RoleId = p.RoleId,
                    PermissionKey = p.PermissionKey,
                    PermissionName = p.PermissionName,
                    ModuleName = p.ModuleName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> SaveRoleFunctionPermissionsAsync(SaveRoleFunctionPermissionsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 删除旧权限
                var existing = await _dbContext.Set<RoleFunctionPermission>()
                    .Where(p => p.RoleId == request.RoleId)
                    .ToListAsync(cancellationToken);

                _dbContext.Set<RoleFunctionPermission>().RemoveRange(existing);

                // 从所有权限定义中查找匹配的权限项
                var allDefinitions = await GetAllPermissionDefinitionsAsync(cancellationToken);
                var permissionLookup = allDefinitions
                    .SelectMany(d => d.Permissions.Select(p => new { p.Key, p.Name, d.ModuleName }))
                    .ToDictionary(p => p.Key, p => p);

                // 添加新权限
                foreach (var key in request.PermissionKeys)
                {
                    if (permissionLookup.TryGetValue(key, out var def))
                    {
                        _dbContext.Set<RoleFunctionPermission>().Add(new RoleFunctionPermission
                        {
                            RoleId = request.RoleId,
                            PermissionKey = key,
                            PermissionName = def.Name,
                            ModuleName = def.ModuleName
                        });
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                Log.Information("成功保存角色 {RoleId} 的功能权限，共 {Count} 项", request.RoleId, request.PermissionKeys.Count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存角色功能权限失败");
                return false;
            }
        }

        public async Task<List<RoleDataPermissionDto>> GetRoleDataPermissionsAsync(int roleId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<RoleDataPermission>()
                .Where(p => p.RoleId == roleId)
                .Select(p => new RoleDataPermissionDto
                {
                    Id = p.Id,
                    RoleId = p.RoleId,
                    DataScope = p.DataScope,
                    DepartmentId = p.DepartmentId,
                    ResourceType = p.ResourceType
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> SaveRoleDataPermissionsAsync(SaveRoleDataPermissionsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 删除旧数据权限
                var existing = await _dbContext.Set<RoleDataPermission>()
                    .Where(p => p.RoleId == request.RoleId)
                    .ToListAsync(cancellationToken);

                _dbContext.Set<RoleDataPermission>().RemoveRange(existing);

                // 添加新数据权限
                foreach (var perm in request.Permissions)
                {
                    _dbContext.Set<RoleDataPermission>().Add(new RoleDataPermission
                    {
                        RoleId = request.RoleId,
                        DataScope = perm.DataScope,
                        DepartmentId = perm.DepartmentId,
                        ResourceType = perm.ResourceType
                    });
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                Log.Information("成功保存角色 {RoleId} 的数据权限", request.RoleId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存角色数据权限失败");
                return false;
            }
        }

        public async Task<List<int>> GetRoleMemberIdsAsync(int roleId, CancellationToken cancellationToken = default)
        {
            // 使用 ASP.NET Identity 的 UserRole 关联表
            return await _dbContext.Set<IdentityUserRole<int>>()
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> SaveRoleMembersAsync(SaveRoleMembersRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
                if (role == null) return false;

                // 获取当前角色成员
                var currentUserIds = await _dbContext.Set<IdentityUserRole<int>>()
                    .Where(ur => ur.RoleId == request.RoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync(cancellationToken);

                var toAdd = request.UserIds.Except(currentUserIds).ToList();
                var toRemove = currentUserIds.Except(request.UserIds).ToList();

                // 移除不再属于该角色的用户
                foreach (var userId in toRemove)
                {
                    var user = await _userManager.FindByIdAsync(userId.ToString());
                    if (user != null)
                    {
                        await _userManager.RemoveFromRoleAsync(user, role.Name!);
                    }
                }

                // 添加新成员
                foreach (var userId in toAdd)
                {
                    var user = await _userManager.FindByIdAsync(userId.ToString());
                    if (user != null)
                    {
                        await _userManager.AddToRoleAsync(user, role.Name!);
                    }
                }

                Log.Information("成功保存角色 {RoleId} 的成员，新增 {AddCount} 人，移除 {RemoveCount} 人",
                    request.RoleId, toAdd.Count, toRemove.Count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存角色成员失败");
                return false;
            }
        }
    }
}
