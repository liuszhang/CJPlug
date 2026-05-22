using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.UserManageApi.Services
{
    public class RoleManageService : IRoleManageService
    {
        private readonly RoleManager<UserRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleManageService(RoleManager<UserRole> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // ---- Public Static Mapping Methods ----

        /// <summary>
        /// Map UserRole entity to RoleManageDto
        /// </summary>
        public static RoleManageDto MapToDto(UserRole r)
        {
            return new RoleManageDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RoleType = r.RoleType,
                IsSystem = r.IsSystem,
                Status = (DataStatus)r.Status,
                CreatedAt = DateTimeOffset.MinValue
            };
        }

        /// <summary>
        /// Map CreateRoleRequest to UserRole entity
        /// </summary>
        public static UserRole MapToEntity(CreateRoleRequest request)
        {
            return new UserRole
            {
                Name = request.Name,
                Description = request.Description,
                RoleType = string.IsNullOrEmpty(request.RoleType) ? "自定义角色" : request.RoleType,
                Status = (int)request.Status
            };
        }

        /// <summary>
        /// Apply UpdateRoleRequest fields to UserRole entity
        /// </summary>
        public static void ApplyUpdate(UserRole role, UpdateRoleRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Name))
                role.Name = request.Name;
            if (request.Description != null)
                role.Description = request.Description;
            if (request.RoleType != null)
                role.RoleType = request.RoleType;
            role.Status = (int)request.Status;
        }

        // ---- Service Methods ----

        public async Task<List<RoleManageDto>> GetAllAsync()
        {
            var roles = await _roleManager.Roles
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return roles.Select(MapToDto).ToList();
        }

        public async Task<RoleManageDto?> GetByIdAsync(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return null;

            return MapToDto(role);
        }

        public async Task<RoleManageDto?> CreateAsync(CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("角色名称不能为空");

            var role = MapToEntity(request);

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                Log.Error("创建角色失败: {Errors}", errors);
                throw new InvalidOperationException($"创建角色失败: {errors}");
            }

            var dto = MapToDto(role);
            dto.CreatedAt = DateTimeOffset.Now;
            return dto;
        }

        public async Task<RoleManageDto?> UpdateAsync(UpdateRoleRequest request)
        {
            var role = await _roleManager.FindByIdAsync(request.Id.ToString());
            if (role == null) return null;

            // 系统角色不允许编辑
            if (role.IsSystem)
            {
                Log.Warning("系统角色 {RoleName} 不允许编辑", role.Name);
                throw new InvalidOperationException($"系统角色 \"{role.Name}\" 不允许编辑");
            }

            ApplyUpdate(role, request);

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                Log.Error("更新角色失败: {Errors}", errors);
                throw new InvalidOperationException($"更新角色失败: {errors}");
            }

            return MapToDto(role);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return false;

            // 系统角色不允许删除
            if (role.IsSystem)
            {
                Log.Warning("系统角色 {RoleName} 不允许删除", role.Name);
                throw new InvalidOperationException($"系统角色 \"{role.Name}\" 不允许删除");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                Log.Error("删除角色失败: {Errors}", errors);
                throw new InvalidOperationException($"删除角色失败: {errors}");
            }

            return true;
        }

        public async Task<bool> AddRoleToUserAsync(AddRoleToUserRequest request)
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role?.Name == null) return false;

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null) return false;

            var result = await _userManager.AddToRoleAsync(user, role.Name);
            return result.Succeeded;
        }

        public async Task<List<RoleUserInfo>> GetRoleUsersAsync(int roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role?.Name == null) return [];

            var users = await _userManager.GetUsersInRoleAsync(role.Name);
            return users.Select(u => new RoleUserInfo
            {
                UserId = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Status = (DataStatus)u.Status
            }).ToList();
        }

        public async Task<bool> RemoveRoleFromUserAsync(RemoveRoleUserRequest request)
        {
            var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
            if (role?.Name == null) return false;

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null) return false;

            var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
            return result.Succeeded;
        }
    }
}
