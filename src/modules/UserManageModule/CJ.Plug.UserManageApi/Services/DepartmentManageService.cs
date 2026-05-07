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

        public async Task<List<DepartmentManageDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // SQLite does not support DateTimeOffset in ORDER BY, use Id instead
            return await _dbContext.Set<Department>()
                .OrderByDescending(d => d.Id)
                .Select(d => new DepartmentManageDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Code = d.Code,
                    ParentId = d.ParentId,
                    ParentName = d.ParentName,
                    Manager = d.Manager,
                    Status = (DataStatus)d.Status,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<DepartmentManageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var dept = await _dbContext.Set<Department>().FindAsync(new object[] { id }, cancellationToken);
            if (dept == null) return null;

            return new DepartmentManageDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Code = dept.Code,
                ParentId = dept.ParentId,
                ParentName = dept.ParentName,
                Manager = dept.Manager,
                Status = (DataStatus)dept.Status,
                CreatedAt = dept.CreatedAt
            };
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

            var dept = new Department
            {
                Name = request.Name,
                Code = request.Code,
                ParentId = request.ParentId,
                ParentName = parentName,
                Manager = request.Manager,
                Status = (int)request.Status,
                CreatedAt = DateTimeOffset.Now
            };

            _dbContext.Set<Department>().Add(dept);
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("成功创建部门：{Name}", request.Name);

            return new DepartmentManageDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Code = dept.Code,
                ParentId = dept.ParentId,
                ParentName = dept.ParentName,
                Manager = dept.Manager,
                Status = (DataStatus)dept.Status,
                CreatedAt = dept.CreatedAt
            };
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

            if (!string.IsNullOrWhiteSpace(request.Name))
                dept.Name = request.Name;
            if (request.Code != null)
                dept.Code = request.Code;
            dept.ParentId = request.ParentId;
            dept.ParentName = parentName;
            if (request.Manager != null)
                dept.Manager = request.Manager;
            dept.Status = (int)request.Status;

            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("成功更新部门：{Name}", dept.Name);

            return new DepartmentManageDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Code = dept.Code,
                ParentId = dept.ParentId,
                ParentName = dept.ParentName,
                Manager = dept.Manager,
                Status = (DataStatus)dept.Status,
                CreatedAt = dept.CreatedAt
            };
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
    }
}
