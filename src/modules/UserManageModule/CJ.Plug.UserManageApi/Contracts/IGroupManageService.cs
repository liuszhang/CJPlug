using CJ.Plug.Models.Contracts;
using CJ.Plug.UserManageModels;

namespace CJ.Plug.UserManageApi.Contracts;

/// <summary>
/// 用户组管理服务接口
/// </summary>
public interface IGroupManageService
{
    Task<List<GroupManageDto>> GetAllAsync();
    Task<GroupManageDto?> GetByIdAsync(int id);
    Task<GroupManageDto?> CreateAsync(CreateGroupRequest request);
    Task<GroupManageDto?> UpdateAsync(UpdateGroupRequest request);
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// 获取用户组成员列表
    /// </summary>
    Task<List<GroupUserInfo>> GetGroupMembersAsync(int groupId);

    /// <summary>
    /// 添加用户到用户组
    /// </summary>
    Task<bool> AddGroupUserAsync(AddGroupUserRequest request);

    /// <summary>
    /// 从用户组移除用户
    /// </summary>
    Task<bool> RemoveGroupUserAsync(RemoveGroupUserRequest request);

    /// <summary>
    /// 获取用户所属用户组
    /// </summary>
    Task<List<UserGroupInfo>> GetUserGroupsAsync(int userId);
}
