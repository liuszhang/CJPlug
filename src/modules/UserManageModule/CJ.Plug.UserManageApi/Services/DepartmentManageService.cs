using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.UserManageApi.Services
{
    public class DepartmentManageService : IDepartmentManageService
    {
        private readonly MainDbContext _dbContext;

        public DepartmentManageService(MainDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ---- Public Static Mapping Methods ----

        /// <summary>
        /// Map Department entity to DepartmentManageDto
        /// </summary>
        public static DepartmentManageDto MapToDto(Department d)
        {
            return new DepartmentManageDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                ParentId = d.ParentId,
                ParentName = d.ParentName,
                Manager = d.Manager,
                Status = (DataStatus)d.Status,
                Creator = d.Creator,
                CreatedAt = d.CreatedAt
            };
        }

        /// <summary>
        /// Map CreateDepartmentRequest to Department entity
        /// </summary>
        public static Department MapToEntity(CreateDepartmentRequest request, string? parentName = null)
        {
            return new Department
            {
                Name = request.Name,
                Code = request.Code,
                ParentId = request.ParentId,
                ParentName = parentName,
                Manager = request.Manager,
                Status = (int)request.Status,
                Creator = request.Creator,
                CreatedAt = DateTimeOffset.Now
            };
        }

        /// <summary>
        /// Apply UpdateDepartmentRequest fields to Department entity
        /// </summary>
        public static void ApplyUpdate(Department dept, UpdateDepartmentRequest request, string? parentName = null)
        {
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                dept.Name = request.Name;
            }
            if (request.Code != null)
                dept.Code = request.Code;
            // 仅在提供Name时才更新ParentId（表示完整编辑，非仅状态变更）
            // 避免审批激活时仅传Status导致ParentId被意外清空
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                dept.ParentId = request.ParentId;
                dept.ParentName = parentName;
            }
            if (request.Manager != null)
                dept.Manager = request.Manager;
            dept.Status = (int)request.Status;
        }

        /// <summary>
        /// Map User entity to DepartmentUserInfo DTO
        /// </summary>
        public static DepartmentUserInfo MapToDepartmentUserInfo(User u)
        {
            return new DepartmentUserInfo
            {
                Id = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                DepartmentId = u.DepartmentId,
                Status = (DataStatus)u.Status
            };
        }

        // ---- Service Methods ----

        public async Task<List<DepartmentManageDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // SQLite does not support DateTimeOffset in ORDER BY, use Id instead
            return await _dbContext.Set<Department>()
                .OrderByDescending(d => d.Id)
                .Select(d => MapToDto(d))
                .ToListAsync(cancellationToken);
        }

        public async Task<DepartmentManageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var dept = await _dbContext.Set<Department>().FindAsync(new object[] { id }, cancellationToken);
            if (dept == null) return null;

            return MapToDto(dept);
        }

        public async Task<DepartmentManageDto?> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("部门名称不能为空");

            // 如果有ParentId，查找父部门名称
            string? parentName = null;
            if (request.ParentId.HasValue)
            {
                var parent = await _dbContext.Set<Department>().FindAsync(new object[] { request.ParentId.Value }, cancellationToken);
                parentName = parent?.Name;
            }

            var dept = MapToEntity(request, parentName);

            _dbContext.Set<Department>().Add(dept);
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("成功创建部门：{Name} by {Creator}", request.Name, request.Creator);

            return MapToDto(dept);
        }

        public async Task<DepartmentManageDto?> UpdateAsync(UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            var dept = await _dbContext.Set<Department>().FindAsync(new object[] { request.Id }, cancellationToken);
            if (dept == null) return null;

            // 如果有ParentId，查找父部门名称
            string? parentName = null;
            if (request.ParentId.HasValue)
            {
                var parent = await _dbContext.Set<Department>().FindAsync(new object[] { request.ParentId.Value }, cancellationToken);
                parentName = parent?.Name;
            }

            ApplyUpdate(dept, request, parentName);

            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("成功更新部门：{Name}", dept.Name);

            return MapToDto(dept);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var dept = await _dbContext.Set<Department>().FindAsync(new object[] { id }, cancellationToken);
            if (dept == null) return false;

            _dbContext.Set<Department>().Remove(dept);
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("成功删除部门：{Name}", dept.Name);
            return true;
        }

        public async Task<List<DepartmentUserInfo>> GetDepartmentUsersAsync(int departmentId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Where(u => u.DepartmentId == departmentId)
                .OrderByDescending(u => u.Id)
                .Select(u => MapToDepartmentUserInfo(u))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> AddUserToDepartmentAsync(int departmentId, int userId, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null) return false;

            user.DepartmentId = departmentId;
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("成功将用户 {UserName} 添加到部门ID: {DepartmentId}", user.UserName, departmentId);
            return true;
        }

        public async Task<bool> RemoveUserFromDepartmentAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null) return false;

            user.DepartmentId = null;
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("成功将用户 {UserName} 从部门移除", user.UserName);
            return true;
        }
    }
}
