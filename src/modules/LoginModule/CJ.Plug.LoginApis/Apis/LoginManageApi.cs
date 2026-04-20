using CJ.Plug.LoginApis.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.LoginApis.Apis
{
    public static class LoginManageApi
    {
        public static IEndpointRouteBuilder MapUserManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/user").WithTags("用户管理");

            //api.MapGet("/getAllUsers", async ([FromServices] IUserService service) => await service.GetAllUsers());

            api.MapPost("/sigup", async ([FromServices] ILoginService service, [FromBody] User request) => await service.RegisterUserAsync(request));
            api.MapPost("/sigin", async ([FromServices] ILoginService service, [FromBody] User request) => await service.LoginUserAsync(request));
            api.MapGet("/logout/{userId}", async ([FromServices] ILoginService service, string userId) => await service.LogoutUserAsync(userId));



            return app;
        }

    }
}
