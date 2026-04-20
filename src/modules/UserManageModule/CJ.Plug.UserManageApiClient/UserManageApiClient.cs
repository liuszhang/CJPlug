using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace CJ.Plug.UserManageApiClient
{
    public partial class UserManageApiClient : BaseApiClient, IUserManageApiClient
    {
        public UserManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }

        public async Task<IEnumerable<User?>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            //var result = httpClient.GetFromJsonAsAsyncEnumerable<User>("/api/user/getAllUsers", cancellationToken);
            //return (IEnumerable<User?>)result;
            var users = await httpClient.GetFromJsonAsync<IEnumerable<User?>>(
                requestUri: "/api/user/getAllUsers",
                cancellationToken: cancellationToken
            );

            // 兜底处理：防止接口返回null时，调用方遍历报空引用异常（可选但推荐）
            return users ?? Enumerable.Empty<User?>();
        }
        
    }
}
