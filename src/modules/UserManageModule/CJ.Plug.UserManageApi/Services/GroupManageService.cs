using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using CJ.Plug.UserManageModels;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.UserManageApi.Services;

public class GroupManageService : IGroupManageService
{
    private readonly MainDbContext _dbContext;

    public GroupManageService(MainDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ---- Public Static Mapping Methods ----

    public static GroupManageDto MapToDto(UserGroup g, int memberCount = 0)
    {
        return new GroupManageDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            IsSystem = g.IsSystem,
            Status = (DataStatus)g.Status,
            CreatedAt = g.CreatedAt,
            Creator = g.Creator,
            MemberCount = memberCount
        };
    }

    public static GroupUserInfo MapToGroupUserInfo(User u)
    {
        return new GroupUserInfo
        {
            UserId = u.Id,
            UserName = u.UserName,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Status = (DataStatus)u.Status
        };
    }

    public static UserGroupInfo MapToUserGroupInfo(UserGroup g)
    {
        return new UserGroupInfo
        {
            GroupId = g.Id,
            GroupName = g.Name,
            GroupDescription = g.Description
        };
    }

    // ---- Service Methods ----

    public async Task<List<GroupManageDto>> GetAllAsync()
    {
        var groups = await _dbContext.UserGroups
            .OrderByDescending(g => g.Id)
            .ToListAsync();

        // 批量获取所有用户组的成员计数
        Dictionary<int, int> memberCounts;
        try
        {
            var groupIds = groups.Select(g => g.Id).ToList();
            memberCounts = await _dbContext.UserGroupMembers
                .Where(m => groupIds.Contains(m.UserGroupId))
                .GroupBy(m => m.UserGroupId)
                .Select(g => new { UserGroupId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.UserGroupId, v => v.Count);
        }
        catch
        {
            memberCounts = new Dictionary<int, int>();
        }

        return groups.Select(g => MapToDto(g, memberCounts.GetValueOrDefault(g.Id, 0))).ToList();
    }

    public async Task<GroupManageDto?> GetByIdAsync(int id)
    {
        var group = await _dbContext.UserGroups.FindAsync(id);
        if (group == null) return null;

        int memberCount;
        try
        {
            memberCount = await _dbContext.UserGroupMembers
                .CountAsync(m => m.UserGroupId == id);
        }
        catch
        {
            memberCount = 0;
        }

        return MapToDto(group, memberCount);
    }

    public async Task<GroupManageDto?> CreateAsync(CreateGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("用户组名称不能为空");

        var group = new UserGroup
        {
            Name = request.Name,
            Description = request.Description,
            Status = (int)request.Status,
            Creator = request.Creator,
            CreatedAt = DateTimeOffset.Now
        };

        _dbContext.UserGroups.Add(group);
        await _dbContext.SaveChangesAsync();

        var dto = MapToDto(group);
        dto.MemberCount = 0;
        return dto;
    }

    public async Task<GroupManageDto?> UpdateAsync(UpdateGroupRequest request)
    {
        var group = await _dbContext.UserGroups.FindAsync(request.Id);
        if (group == null) return null;

        if (group.IsSystem)
        {
            Log.Warning("系统用户组 {GroupName} 不允许编辑", group.Name);
            throw new InvalidOperationException($"系统用户组 \"{group.Name}\" 不允许编辑");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            group.Name = request.Name;
        if (request.Description != null)
            group.Description = request.Description;
        group.Status = (int)request.Status;

        await _dbContext.SaveChangesAsync();

        return MapToDto(group);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await _dbContext.UserGroups.FindAsync(id);
        if (group == null) return false;

        if (group.IsSystem)
        {
            Log.Warning("系统用户组 {GroupName} 不允许删除", group.Name);
            throw new InvalidOperationException($"系统用户组 \"{group.Name}\" 不允许删除");
        }

        _dbContext.UserGroups.Remove(group);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<GroupUserInfo>> GetGroupMembersAsync(int groupId)
    {
        var group = await _dbContext.UserGroups.FindAsync(groupId);
        if (group == null)
            throw new InvalidOperationException($"用户组 ID {groupId} 不存在");

        var memberIds = await _dbContext.UserGroupMembers
            .Where(m => m.UserGroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();

        if (memberIds.Count == 0)
            return new List<GroupUserInfo>();

        var users = await _dbContext.Users
            .Where(u => memberIds.Contains(u.Id))
            .ToListAsync();

        return users.Select(MapToGroupUserInfo).ToList();
    }

    public async Task<bool> AddGroupUserAsync(AddGroupUserRequest request)
    {
        var group = await _dbContext.UserGroups.FindAsync(request.GroupId);
        if (group == null)
            throw new InvalidOperationException($"用户组 ID {request.GroupId} 不存在");

        var user = await _dbContext.Users.FindAsync(request.UserId);
        if (user == null)
            throw new InvalidOperationException($"用户 ID {request.UserId} 不存在");

        var existing = await _dbContext.UserGroupMembers
            .AnyAsync(m => m.UserId == request.UserId && m.UserGroupId == request.GroupId);

        if (existing)
        {
            Log.Warning("用户 {UserId} 已在用户组 {GroupId} 中", request.UserId, request.GroupId);
            return false;
        }

        _dbContext.UserGroupMembers.Add(new UserGroupMember
        {
            UserId = request.UserId,
            UserGroupId = request.GroupId,
            JoinedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveGroupUserAsync(RemoveGroupUserRequest request)
    {
        var member = await _dbContext.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.UserGroupId == request.GroupId);

        if (member == null) return false;

        _dbContext.UserGroupMembers.Remove(member);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserGroupInfo>> GetUserGroupsAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"用户 ID {userId} 不存在");

        var groupIds = await _dbContext.UserGroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.UserGroupId)
            .ToListAsync();

        if (groupIds.Count == 0)
            return new List<UserGroupInfo>();

        var groups = await _dbContext.UserGroups
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync();

        return groups.Select(MapToUserGroupInfo).ToList();
    }
}
