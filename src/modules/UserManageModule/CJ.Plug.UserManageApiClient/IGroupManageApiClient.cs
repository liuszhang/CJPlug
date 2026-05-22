using CJ.Plug.UserManageModels;
using System.Net.Http.Json;

namespace CJ.Plug.UserManageApiClient;

public interface IGroupManageApiClient
{
    Task<List<GroupManageDto>> GetAllUserGroupAsync();
    Task<GroupManageDto?> GetUserGroupByIdAsync(int id);
    Task<GroupManageDto?> CreateUserGroupAsync(CreateGroupRequest request);
    Task<GroupManageDto?> UpdateUserGroupAsync(UpdateGroupRequest request);
    Task<bool> DeleteUserGroupAsync(int id);
    Task<List<GroupUserInfo>> GetGroupMembersAsync(int groupId);
    Task<bool> AddGroupUserAsync(AddGroupUserRequest request);
    Task<bool> RemoveGroupUserAsync(RemoveGroupUserRequest request);
    Task<List<UserGroupInfo>> GetUserGroupsAsync(int userId);
}
