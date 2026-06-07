using CJ.Plug.AuthApi.DbContext;
using CJ.Plug.AuthApi.Contracts;
using CJ.Plug.AuthModels;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;

namespace CJ.Plug.AuthApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly MainDbContext _dbContext;
        private readonly IUserManageService _userManageService;
        private readonly IRoleManageService _roleManageService;
        private readonly IDepartmentManageService _departmentManageService;
        private readonly IGroupManageService _groupManageService;

        public AuthService(
            MainDbContext dbContext,
            IUserManageService userManageService,
            IRoleManageService roleManageService,
            IDepartmentManageService departmentManageService,
            IGroupManageService groupManageService)
        {
            _dbContext = dbContext;
            _userManageService = userManageService;
            _roleManageService = roleManageService;
            _departmentManageService = departmentManageService;
            _groupManageService = groupManageService;
        }

        public async Task<List<AuthRequestDto>> GetAllAsync(AuthRequestStatus? status = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Set<AuthRequestEntity>().AsQueryable();
            
            if (status.HasValue)
                query = query.Where(x => x.Status == (int)status.Value);

            return await query
                .OrderByDescending(x => x.RequestedAt)
                .Select(x => MapToDto(x))
                .ToListAsync(cancellationToken);
        }

        public async Task<AuthRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Set<AuthRequestEntity>().FindAsync(new object[] { id }, cancellationToken);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<AuthRequestDto> CreateAsync(CreateAuthRequestDto request, CancellationToken cancellationToken = default)
        {
            var entity = MapToEntity(request);

            // 创建操作：立即创建数据（状态=授权中），并记录TargetId
            var operationType = request.OperationType;
            if (IsCreateOperation(operationType))
            {
                var targetId = await CreateTargetDataAsync(operationType, request.OperationData, request.RequestedBy, cancellationToken);
                if (targetId > 0)
                {
                    entity.TargetId = targetId;
                }
                else
                {
                    Log.Error("创建目标数据失败: OperationType={OperationType}, OperationData={OperationData}",
                        operationType, request.OperationData);
                    throw new InvalidOperationException($"创建数据失败: {operationType}");
                }
            }

            // 启用/禁用/锁定/解锁操作：记录目标用户ID到TargetId，审批通过后才执行变更
            if (IsUserStatusOrLockOperation(operationType))
            {
                var targetId = ExtractTargetIdFromOperationData(request.OperationData);
                if (targetId > 0)
                {
                    entity.TargetId = targetId;
                }
            }

            _dbContext.Set<AuthRequestEntity>().Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("创建授权请求：{Operation} - {Target} by {User}", 
                request.OperationType, request.TargetDescription, request.RequestedBy);

            return MapToDto(entity);
        }

        public async Task<AuthRequestDto?> ApproveAsync(ApproveAuthRequestDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Set<AuthRequestEntity>().FindAsync(new object[] { request.RequestId }, cancellationToken);
            if (entity == null || entity.Status != 0) return null;

            if (request.IsApproved)
            {
                // 批准：将状态改为启用或执行变更操作
                var success = await ActivateTargetDataAsync((AuthOperationType)entity.OperationType, entity.TargetId, cancellationToken);
                if (!success)
                {
                    Log.Error("激活/执行数据失败：{Id}", request.RequestId);
                    return null;
                }
            }
            else
            {
                // 拒绝：启用/禁用/锁定/解锁操作被拒绝时无需删除数据，仅更新授权状态
                if (!IsUserStatusOrLockOperation((AuthOperationType)entity.OperationType))
                {
                    // 其他操作（创建/更新/删除）：删除数据
                    var success = await DeleteTargetDataAsync((AuthOperationType)entity.OperationType, entity.TargetId, cancellationToken);
                    if (!success)
                    {
                        Log.Error("删除数据失败：{Id}", request.RequestId);
                        // 即使删除失败也继续更新授权状态
                    }
                }
            }

            entity.Status = request.IsApproved ? 1 : 2; // Approved or Rejected
            entity.ApprovedBy = request.ApprovedBy;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.Remark = request.Remark;

            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("审批授权请求：{Id} - {Status} by {User}", 
                request.RequestId, request.IsApproved ? "批准" : "拒绝", request.ApprovedBy);

            return MapToDto(entity);
        }

        public async Task<AuthRequestDto?> CancelAsync(int id, string cancelledBy, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Set<AuthRequestEntity>().FindAsync(new object[] { id }, cancellationToken);
            if (entity == null || entity.Status != 0) return null;

            // 只有请求人可以撤回
            if (entity.RequestedBy != cancelledBy) return null;

            // 撤回时删除数据（启用/禁用/锁定/解锁操作不需要删除）
            if (entity.TargetId > 0 && !IsUserStatusOrLockOperation((AuthOperationType)entity.OperationType))
            {
                await DeleteTargetDataAsync((AuthOperationType)entity.OperationType, entity.TargetId, cancellationToken);
            }

            entity.Status = 3; // Cancelled
            await _dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("撤回授权请求：{Id} by {User}", id, cancelledBy);

            return MapToDto(entity);
        }

        public async Task<bool> HasPendingRequestAsync(AuthOperationType operationType, string targetDescription, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<AuthRequestEntity>()
                .AnyAsync(x => x.OperationType == (int)operationType 
                    && x.TargetDescription == targetDescription 
                    && x.Status == 0, cancellationToken);
        }

        /// <summary>
        /// 判断是否为创建操作
        /// </summary>
        private static bool IsCreateOperation(AuthOperationType operationType)
        {
            return operationType is AuthOperationType.CreateUser
                or AuthOperationType.CreateRole
                or AuthOperationType.CreateDepartment
                or AuthOperationType.CreateGroup;
        }

        /// <summary>
        /// 判断是否为用户状态/锁定操作（审批通过时执行变更，拒绝/撤回时无需删除数据）
        /// </summary>
        private static bool IsUserStatusOrLockOperation(AuthOperationType operationType)
        {
            return operationType is AuthOperationType.EnableUser
                or AuthOperationType.DisableUser
                or AuthOperationType.LockUser
                or AuthOperationType.UnlockUser;
        }

        /// <summary>
        /// 从操作数据JSON中提取目标ID
        /// </summary>
        private static int ExtractTargetIdFromOperationData(string operationData)
        {
            try
            {
                using var doc = JsonDocument.Parse(operationData);
                if (doc.RootElement.TryGetProperty("UserId", out var userIdElement))
                {
                    return userIdElement.GetInt32();
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 创建目标数据（状态=授权中）
        /// </summary>
        private async Task<int> CreateTargetDataAsync(AuthOperationType operationType, string operationData, string creator, CancellationToken cancellationToken)
        {
            try
            {
                switch (operationType)
                {
                    case AuthOperationType.CreateUser:
                        return await CreateUserDataAsync(operationData, cancellationToken);
                    case AuthOperationType.CreateRole:
                        return await CreateRoleDataAsync(operationData, cancellationToken);
                    case AuthOperationType.CreateDepartment:
                        return await CreateDepartmentDataAsync(operationData, creator, cancellationToken);
                    case AuthOperationType.CreateGroup:
                        return await CreateGroupDataAsync(operationData, creator, cancellationToken);
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建目标数据失败");
                return 0;
            }
        }

        private async Task<int> CreateUserDataAsync(string operationData, CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("开始反序列化用户数据: {OperationData}", operationData);
                var request = JsonSerializer.Deserialize<CreateUserRequest>(operationData);
                if (request == null)
                {
                    Log.Error("JSON 反序列化失败，返回 null: {OperationData}", operationData);
                    return 0;
                }
                
                Log.Information("反序列化成功: UserName={UserName}, Email={Email}, DepartmentId={DepartmentId}", 
                    request.UserName, request.Email, request.DepartmentId);
                
                // 设置状态为授权中
                request.Status = DataStatus.Authorizing;
                Log.Information("调用 UserManageService.CreateUserAsync");
                var user = await _userManageService.CreateUserAsync(request, cancellationToken);
                
                if (user == null)
                {
                    Log.Error("UserManageService.CreateUserAsync 返回 null");
                    return 0;
                }
                
                Log.Information("用户创建成功: Id={Id}, UserName={UserName}", user.Id, user.UserName);
                return user.Id;
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "JSON 反序列化异常: {OperationData}", operationData);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CreateUserDataAsync 发生异常");
                return 0;
            }
        }

        private async Task<int> CreateRoleDataAsync(string operationData, CancellationToken cancellationToken)
        {
            var request = JsonSerializer.Deserialize<CreateRoleRequest>(operationData);
            if (request == null) return 0;
            
            // 设置状态为授权中
            request.Status = DataStatus.Authorizing;
            var role = await _roleManageService.CreateAsync(request);
            return role?.Id ?? 0;
        }

        private async Task<int> CreateDepartmentDataAsync(string operationData, string creator, CancellationToken cancellationToken)
        {
            var request = JsonSerializer.Deserialize<CreateDepartmentRequest>(operationData);
            if (request == null) return 0;
            
            // 设置状态为授权中，创建人从 RequestedBy 传入
            request.Status = DataStatus.Authorizing;
            request.Creator = creator;
            var dept = await _departmentManageService.CreateAsync(request, cancellationToken);
            return dept?.Id ?? 0;
        }

        private async Task<int> CreateGroupDataAsync(string operationData, string creator, CancellationToken cancellationToken)
        {
            var request = JsonSerializer.Deserialize<CreateGroupRequest>(operationData);
            if (request == null) return 0;

            request.Status = DataStatus.Authorizing;
            request.Creator = creator;
            var group = await _groupManageService.CreateAsync(request);
            return group?.Id ?? 0;
        }

        /// <summary>
        /// 激活目标数据（状态改为启用）或执行变更操作
        /// </summary>
        private async Task<bool> ActivateTargetDataAsync(AuthOperationType operationType, int targetId, CancellationToken cancellationToken)
        {
            try
            {
                switch (operationType)
                {
                    case AuthOperationType.CreateUser:
                        return await ActivateUserAsync(targetId, cancellationToken);
                    case AuthOperationType.CreateRole:
                        return await ActivateRoleAsync(targetId, cancellationToken);
                    case AuthOperationType.CreateDepartment:
                        return await ActivateDepartmentAsync(targetId, cancellationToken);
                    case AuthOperationType.CreateGroup:
                        return await ActivateGroupAsync(targetId, cancellationToken);
                    case AuthOperationType.UpdateUser:
                    case AuthOperationType.UpdateRole:
                    case AuthOperationType.UpdateDepartment:
                    case AuthOperationType.UpdateGroup:
                        // 更新操作直接执行
                        return await ExecuteUpdateOperationAsync(operationType, targetId, cancellationToken);
                    case AuthOperationType.DeleteUser:
                    case AuthOperationType.DeleteRole:
                    case AuthOperationType.DeleteDepartment:
                    case AuthOperationType.DeleteGroup:
                        // 删除操作直接执行
                        return await ExecuteDeleteOperationAsync(operationType, targetId, cancellationToken);
                    case AuthOperationType.EnableUser:
                        return await _userManageService.SetUserStatusAsync(targetId, DataStatus.Active, cancellationToken);
                    case AuthOperationType.DisableUser:
                        return await _userManageService.SetUserStatusAsync(targetId, DataStatus.Disabled, cancellationToken);
                    case AuthOperationType.LockUser:
                        return await _userManageService.SetUserLockoutAsync(targetId, true, cancellationToken);
                    case AuthOperationType.UnlockUser:
                        return await _userManageService.SetUserLockoutAsync(targetId, false, cancellationToken);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "激活/执行目标数据失败");
                return false;
            }
        }

        private async Task<bool> ActivateUserAsync(int userId, CancellationToken cancellationToken)
        {
            // 加载现有用户，保留所有字段
            var existingUser = await _userManageService.GetByIdAsync(userId, cancellationToken);
            if (existingUser == null) return false;

            var updateRequest = new UpdateUserRequest
            {
                Id = userId,
                Status = DataStatus.Active,
                Email = existingUser.Email,
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                PhoneNumber = existingUser.PhoneNumber,
                DepartmentId = existingUser.DepartmentId
            };
            var user = await _userManageService.UpdateUserAsync(updateRequest, cancellationToken);
            return user != null;
        }

        private async Task<bool> ActivateRoleAsync(int roleId, CancellationToken cancellationToken)
        {
            // 加载现有角色，保留所有字段
            var existingRole = await _roleManageService.GetByIdAsync(roleId);
            if (existingRole == null) return false;

            var updateRequest = new UpdateRoleRequest
            {
                Id = roleId,
                Name = existingRole.Name,
                Description = existingRole.Description,
                RoleType = existingRole.RoleType,
                Status = DataStatus.Active
            };
            var role = await _roleManageService.UpdateAsync(updateRequest);
            return role != null;
        }

        private async Task<bool> ActivateDepartmentAsync(int deptId, CancellationToken cancellationToken)
        {
            // 加载现有部门，保留所有字段（特别是 ParentId，否则树结构会被破坏）
            var existingDept = await _departmentManageService.GetByIdAsync(deptId, cancellationToken);
            if (existingDept == null) return false;

            var updateRequest = new UpdateDepartmentRequest
            {
                Id = deptId,
                Name = existingDept.Name,
                Code = existingDept.Code,
                ParentId = existingDept.ParentId,
                Manager = existingDept.Manager,
                Status = DataStatus.Active
            };
            var dept = await _departmentManageService.UpdateAsync(updateRequest, cancellationToken);
            return dept != null;
        }

        private async Task<bool> ActivateGroupAsync(int groupId, CancellationToken cancellationToken)
        {
            var existingGroup = await _groupManageService.GetByIdAsync(groupId);
            if (existingGroup == null) return false;

            var updateRequest = new UpdateGroupRequest
            {
                Id = groupId,
                Name = existingGroup.Name,
                Description = existingGroup.Description,
                Status = DataStatus.Active
            };
            var group = await _groupManageService.UpdateAsync(updateRequest);
            return group != null;
        }

        /// <summary>
        /// 删除目标数据
        /// </summary>
        private async Task<bool> DeleteTargetDataAsync(AuthOperationType operationType, int targetId, CancellationToken cancellationToken)
        {
            try
            {
                if (targetId <= 0) return true; // 没有关联数据

                return operationType switch
                {
                    AuthOperationType.CreateUser or AuthOperationType.DeleteUser
                        => await _userManageService.DeleteAsync(targetId, cancellationToken),
                    AuthOperationType.CreateRole or AuthOperationType.DeleteRole
                        => await _roleManageService.DeleteAsync(targetId),
                    AuthOperationType.CreateDepartment or AuthOperationType.DeleteDepartment
                        => await _departmentManageService.DeleteAsync(targetId, cancellationToken),
                    AuthOperationType.CreateGroup or AuthOperationType.DeleteGroup
                        => await _groupManageService.DeleteAsync(targetId),
                    _ => true
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "删除目标数据失败");
                return false;
            }
        }

        /// <summary>
        /// 执行更新操作
        /// </summary>
        private async Task<bool> ExecuteUpdateOperationAsync(AuthOperationType operationType, int targetId, CancellationToken cancellationToken)
        {
            // 更新操作的逻辑（暂未实现完整）
            return true;
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        private async Task<bool> ExecuteDeleteOperationAsync(AuthOperationType operationType, int targetId, CancellationToken cancellationToken)
        {
            return operationType switch
            {
                AuthOperationType.DeleteUser => await _userManageService.DeleteAsync(targetId, cancellationToken),
                AuthOperationType.DeleteRole => await _roleManageService.DeleteAsync(targetId),
                AuthOperationType.DeleteDepartment => await _departmentManageService.DeleteAsync(targetId, cancellationToken),
                AuthOperationType.DeleteGroup => await _groupManageService.DeleteAsync(targetId),
                _ => false
            };
        }

        /// <summary>
        /// Map CreateAuthRequestDto to AuthRequestEntity
        /// </summary>
        public static AuthRequestEntity MapToEntity(CreateAuthRequestDto request)
        {
            return new AuthRequestEntity
            {
                OperationType = (int)request.OperationType,
                TargetDescription = request.TargetDescription,
                OperationData = request.OperationData,
                RequestedBy = request.RequestedBy,
                RequestedAt = DateTime.UtcNow,
                Status = 0 // Pending
            };
        }

        public async Task<bool> UnlockSystemAdminAsync(UnlockSystemAdminRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 查找用户
                var user = await _userManageService.GetAllUsersAsync(cancellationToken);
                var targetUser = user.FirstOrDefault(u => u.UserName == request.UserName);
                if (targetUser == null)
                {
                    Log.Warning("解锁系统管理员失败：用户 {UserName} 不存在", request.UserName);
                    return false;
                }

                // 检查是否为系统管理员
                var roles = await _userManageService.GetUserRolesAsync(targetUser.Id, cancellationToken);
                Log.Information("用户 {UserName} 的角色列表: {@Roles}", request.UserName, roles);
                var isSystemAdmin = roles.Any(r => r == "SystemAdmin" || r == "系统管理员" || r.Contains("SystemAdmin") || r.Contains("系统管理员"));
                if (!isSystemAdmin)
                {
                    Log.Warning("解锁系统管理员失败：用户 {UserName} 不是系统管理员，实际角色: {@Roles}", request.UserName, roles);
                    return false;
                }

                // 检查是否已锁定（通过 LockoutEnd 判断，而非 IsLocked 自定义字段）
                var isLockedOut = targetUser.LockoutEnabled && targetUser.LockoutEnd.HasValue && targetUser.LockoutEnd.Value > DateTimeOffset.UtcNow;
                if (!isLockedOut)
                {
                    Log.Warning("解锁系统管理员失败：用户 {UserName} 未锁定", request.UserName);
                    return false;
                }

                // 解锁账号
                var unlockResult = await _userManageService.SetUserLockoutAsync(targetUser.Id, false, cancellationToken);
                if (!unlockResult)
                {
                    Log.Error("解锁系统管理员失败：解锁操作失败");
                    return false;
                }

                // 创建审计记录
                var auditRequest = new CreateAuthRequestDto
                {
                    OperationType = AuthOperationType.UnlockUser,
                    TargetDescription = $"解锁系统管理员账号: {request.UserName}",
                    OperationData = JsonSerializer.Serialize(new { 
                        UserName = request.UserName, 
                        UnlockedBy = request.UnlockedBy,
                        Remark = request.Remark 
                    }),
                    RequestedBy = request.UnlockedBy
                };
                await CreateAsync(auditRequest, cancellationToken);

                Log.Information("成功解锁系统管理员账号：{UserName}，操作人：{UnlockedBy}", request.UserName, request.UnlockedBy);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "解锁系统管理员时发生异常");
                return false;
            }
        }

        public static AuthRequestDto MapToDto(AuthRequestEntity entity)
        {
            return new AuthRequestDto
            {
                Id = entity.Id,
                OperationType = (AuthOperationType)entity.OperationType,
                TargetDescription = entity.TargetDescription,
                OperationData = entity.OperationData,
                TargetId = entity.TargetId,
                RequestedBy = entity.RequestedBy,
                RequestedAt = entity.RequestedAt,
                ApprovedBy = entity.ApprovedBy,
                ApprovedAt = entity.ApprovedAt,
                Status = (AuthRequestStatus)entity.Status,
                Remark = entity.Remark
            };
        }
    }
}
