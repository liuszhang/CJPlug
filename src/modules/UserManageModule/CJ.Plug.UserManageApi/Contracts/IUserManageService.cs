using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;

namespace CJ.Plug.UserManageApi.Contracts
{
    public interface IUserManageService : IBaseRepositoryService<User, int>
    {
        //Task GetUsers();
    }
}
