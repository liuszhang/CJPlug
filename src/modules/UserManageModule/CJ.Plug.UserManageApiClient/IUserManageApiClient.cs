using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.UserManageApiClient
{
    public interface IUserManageApiClient
    {        
        Task<IEnumerable<User?>> GetAllUsersAsync(CancellationToken cancellationToken = default);        
    }
}
