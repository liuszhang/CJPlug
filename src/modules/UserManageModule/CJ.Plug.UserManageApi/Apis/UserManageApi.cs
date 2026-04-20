using CJ.Plug.Models.Job;
using CJ.Plug.Models.Shared;
using CJ.Plug.UserManageApi.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.UserManageApi.Apis
{
    public static class UserManageApi
    {
        public static IEndpointRouteBuilder MapUserManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/user").WithTags("用户管理");

            api.MapGet("/getAllUsers", async (IUserManageService service) => await service.GetAllAsync());
            
            return app;
        }

    }
}
