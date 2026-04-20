using CJ.Plug.Models.Job;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;
using System.Text.Json;

namespace CJ.Plug.UserManageApi.Services
{
    public partial class UserManageService : BaseRepositoryService<User, int>, IUserManageService
    {
        public UserManageService(MainDbContext dbContext) : base(dbContext)
        {
        }

        //public Task GetUsers()
        //{
        //    throw new NotImplementedException();
        //}
    }

}
