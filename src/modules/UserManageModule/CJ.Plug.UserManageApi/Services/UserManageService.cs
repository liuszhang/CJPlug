using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;
using System.Text.Json;

namespace CJ.Plug.UserManageApi.Services
{
    public partial class UserManageService : BaseRepositoryService<User, int>, IUserManageService
    {
        private readonly UserManager<User> _userManager;

        public UserManageService(MainDbContext dbContext, UserManager<User> userManager) : base(dbContext)
        {
            _userManager = userManager;
        }

        // ---- Public Static Mapping Methods ----

        /// <summary>
        /// Map CreateUserRequest to User entity
        /// </summary>
        public static User MapToEntity(CreateUserRequest request)
        {
            return new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                DepartmentId = request.DepartmentId,
                Status = (int)request.Status,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Apply UpdateUserRequest fields to User entity
        /// </summary>
        public static void ApplyUpdate(User user, UpdateUserRequest request)
        {
            if (request.Email != null)
                user.Email = request.Email;
            if (request.FirstName != null)
                user.FirstName = request.FirstName;
            if (request.LastName != null)
                user.LastName = request.LastName;
            if (request.PhoneNumber != null)
                user.PhoneNumber = request.PhoneNumber;
            if (request.DepartmentId.HasValue)
                user.DepartmentId = request.DepartmentId;
            user.Status = (int)request.Status;
        }

        // ---- Service Methods ----

        public async Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userManager.Users.ToListAsync(cancellationToken);
            foreach (var u in users)
            {
                Log.Information("[GetAllUsers] 用户={UserName}, LockoutEnabled={LockoutEnabled}, LockoutEnd={LockoutEnd}", 
                    u.UserName, u.LockoutEnabled, u.LockoutEnd);
            }
            return users;
        }

        public async Task<User?> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查用户名是否已存在
                var existingUser = await _userManager.FindByNameAsync(request.UserName);
                if (existingUser != null)
                {
                    Log.Warning("创建用户失败：用户名 {UserName} 已存在", request.UserName);
                    return null;
                }

                // 检查邮箱是否已存在（仅非空邮箱才检查唯一性）
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var existingEmail = await _userManager.FindByEmailAsync(request.Email);
                    if (existingEmail != null)
                    {
                        Log.Warning("创建用户失败：邮箱 {Email} 已存在", request.Email);
                        return null;
                    }
                }

                var user = MapToEntity(request);

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        Log.Error("创建用户错误：{Error}", error.Description);
                    }
                    return null;
                }

                // 分配角色
                if (request.RoleNames is { Count: > 0 })
                {
                    var roleResult = await _userManager.AddToRolesAsync(user, request.RoleNames);
                    if (!roleResult.Succeeded)
                    {
                        Log.Warning("为用户 {UserName} 分配角色时部分失败", request.UserName);
                    }
                }

                Log.Information("成功创建用户：{UserName}", request.UserName);
                return user;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建用户时发生异常");
                return null;
            }
        }

        public async Task<User?> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.Id.ToString());
                if (user == null)
                {
                    Log.Warning("更新用户失败：用户 {Id} 不存在", request.Id);
                    return null;
                }

                // 系统用户不允许编辑
                if (user.IsSystem)
                {
                    Log.Warning("系统用户 {UserName} 不允许编辑", user.UserName);
                    throw new InvalidOperationException($"系统用户 \"{user.UserName}\" 不允许编辑");
                }

                ApplyUpdate(user, request);

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        Log.Error("更新用户错误：{Error}", error.Description);
                    }
                    return null;
                }

                // 更新角色
                if (request.RoleNames != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToRemove = currentRoles.Except(request.RoleNames).ToList();
                    var rolesToAdd = request.RoleNames.Except(currentRoles).ToList();

                    if (rolesToRemove.Count > 0)
                        await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (rolesToAdd.Count > 0)
                        await _userManager.AddToRolesAsync(user, rolesToAdd);
                }

                Log.Information("成功更新用户：{UserName}", user.UserName);
                return user;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新用户时发生异常");
                return null;
            }
        }

        public async Task<bool> AssignRolesAsync(AssignRolesRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null) return false;

                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(request.RoleNames).ToList();
                var rolesToAdd = request.RoleNames.Except(currentRoles).ToList();

                if (rolesToRemove.Count > 0)
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (rolesToAdd.Count > 0)
                    await _userManager.AddToRolesAsync(user, rolesToAdd);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "分配角色时发生异常");
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return [];

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<bool> SetUserStatusAsync(int userId, DataStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    Log.Warning("设置用户状态失败：用户 {Id} 不存在", userId);
                    return false;
                }

                if (user.IsSystem)
                {
                    Log.Warning("系统用户 {UserName} 不允许修改状态", user.UserName);
                    throw new InvalidOperationException($"系统用户 \"{user.UserName}\" 不允许修改状态");
                }

                user.Status = (int)status;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        Log.Error("设置用户状态错误：{Error}", error.Description);
                    return false;
                }

                Log.Information("已将用户 {UserName} 状态设置为 {Status}", user.UserName, status.GetDisplayName());
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置用户状态时发生异常");
                return false;
            }
        }

        public async Task<bool> SetUserLockoutAsync(int userId, bool isLocked, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    Log.Warning("设置用户锁定状态失败：用户 {Id} 不存在", userId);
                    return false;
                }

                if (user.IsSystem)
                {
                    Log.Warning("系统用户 {UserName} 不允许锁定/解锁", user.UserName);
                    throw new InvalidOperationException($"系统用户 \"{user.UserName}\" 不允许锁定/解锁");
                }

                if (isLocked)
                {
                    // 锁定：启用 LockoutEnabled 并设置 LockoutEnd 为未来 100 年
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                    Log.Information("锁定用户 {UserName}: LockoutEnabled={LockoutEnabled}, LockoutEnd={LockoutEnd}", 
                        user.UserName, user.LockoutEnabled, user.LockoutEnd);
                }
                else
                {
                    // 解锁：清空 LockoutEnd
                    user.LockoutEnd = null;
                    Log.Information("解锁用户 {UserName}: LockoutEnd={LockoutEnd}", user.UserName, user.LockoutEnd ?? (object)"null");
                }
                
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        Log.Error("设置用户锁定状态错误：{Error}", error.Description);
                    return false;
                }

                Log.Information("已将用户 {UserName} 锁定状态设置为 {IsLocked}", user.UserName, isLocked ? "锁定" : "解锁");
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置用户锁定状态时发生异常");
                return false;
            }
        }
    }
}
