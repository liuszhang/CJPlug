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

        public RoleManageService(RoleManager<UserRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<List<RoleManageDto>> GetAllAsync()
        {
            var roles = await _roleManager.Roles
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return roles.Select(r => new RoleManageDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RoleType = r.RoleType,
                Status = (DataStatus)r.Status,
                CreatedAt = DateTimeOffset.MinValue
            }).ToList();
        }

        public async Task<RoleManageDto?> GetByIdAsync(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return null;

            return new RoleManageDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                RoleType = role.RoleType,
                Status = (DataStatus)role.Status,
                CreatedAt = DateTimeOffset.MinValue
            };
        }

        public async Task<RoleManageDto?> CreateAsync(CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("角色名称不能为空");

            var role = new UserRole
            {
                Name = request.Name,
                Description = request.Description,
                RoleType = string.IsNullOrEmpty(request.RoleType) ? "自定义角色" : request.RoleType,
                Status = (int)request.Status
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                Log.Error("创建角色失败: {Errors}", errors);
                throw new InvalidOperationException($"创建角色失败: {errors}");
            }

            return new RoleManageDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                RoleType = role.RoleType,
                Status = (DataStatus)role.Status,
                CreatedAt = DateTimeOffset.Now
            };
        }

        public async Task<RoleManageDto?> UpdateAsync(UpdateRoleRequest request)
        {
            var role = await _roleManager.FindByIdAsync(request.Id.ToString());
            if (role == null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
                role.Name = request.Name;
            if (request.Description != null)
                role.Description = request.Description;
            if (request.RoleType != null)
                role.RoleType = request.RoleType;
            role.Status = (int)request.Status;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                Log.Error("更新角色失败: {Errors}", errors);
                throw new InvalidOperationException($"更新角色失败: {errors}");
            }

            return new RoleManageDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                RoleType = role.RoleType,
                Status = (DataStatus)role.Status,
                CreatedAt = DateTimeOffset.MinValue
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null) return false;

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                Log.Error("删除角色失败: {Errors}", errors);
                throw new InvalidOperationException($"删除角色失败: {errors}");
            }

            return true;
        }
    }
}
