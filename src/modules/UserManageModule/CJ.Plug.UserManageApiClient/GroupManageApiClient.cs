using CJ.Plug.Models.Plug;
using CJ.Plug.UserManageModels;
using System.Net.Http.Json;

namespace CJ.Plug.UserManageApiClient;

public class GroupManageApiClient : BaseApiClient, IGroupManageApiClient
{
    //private readonly HttpClient httpClient;

    public GroupManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
    {

    }


    public async Task<List<GroupManageDto>> GetAllUserGroupAsync()
    {
        return await httpClient.GetFromJsonAsync<List<GroupManageDto>>("api/group/getAllGroups") ?? [];
    }

    public async Task<GroupManageDto?> GetUserGroupByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<GroupManageDto>($"api/group/getGroupById/{id}");
    }

    public async Task<GroupManageDto?> CreateUserGroupAsync(CreateGroupRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/group/createGroup", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GroupManageDto>();
        }
        return null;
    }

    public async Task<GroupManageDto?> UpdateUserGroupAsync(UpdateGroupRequest request)
    {
        var response = await httpClient.PutAsJsonAsync("api/group/updateGroup", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GroupManageDto>();
        }
        return null;
    }

    public async Task<bool> DeleteUserGroupAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/group/deleteGroup/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<GroupUserInfo>> GetGroupMembersAsync(int groupId)
    {
        return await httpClient.GetFromJsonAsync<List<GroupUserInfo>>($"api/group/getGroupMembers/{groupId}") ?? [];
    }

    public async Task<bool> AddGroupUserAsync(AddGroupUserRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/group/addGroupUser", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveGroupUserAsync(RemoveGroupUserRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/group/removeGroupUser", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserGroupInfo>> GetUserGroupsAsync(int userId)
    {
        return await httpClient.GetFromJsonAsync<List<UserGroupInfo>>($"api/group/getUserGroups/{userId}") ?? [];
    }
}
