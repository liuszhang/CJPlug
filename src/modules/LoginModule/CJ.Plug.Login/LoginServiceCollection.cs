//using CJ.Plug.LoginApiClient.ApiClients;
using CJ.Plug.LoginApiClient.ApiClients;
using CJ.Plug.LoginApis.Apis;
using CJ.Plug.LoginApis.Contracts;
using CJ.Plug.LoginApis.Services;
using CJ.Plug.LoginPages;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Login
{
    public static class LoginServiceCollection
    {
        public static IServiceCollection AddLoginModulePageServices(this IServiceCollection services)
        {            
            services.AddScoped<IModule, LoginModule>();

            services.AddHttpClient<ILoginApiClient, CJ.Plug.LoginApiClient.ApiClients.LoginApiClient>(client => client.BaseAddress = new(GlobalData.MainDispatcherServer));


            return services;
        }

        public static IServiceCollection AddLoginModuleApiServices(this IServiceCollection services)
        {

            services.AddIdentity<User, UserRole>().AddEntityFrameworkStores<MainDbContext>();
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 3;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;

            });

            services.AddScoped<ILoginService, LoginService>();

            services.AddHttpClient<ILoginApiClient, CJ.Plug.LoginApiClient.ApiClients.LoginApiClient>(client => client.BaseAddress = new(GlobalData.MainDispatcherServer));



            return services;
        }

        public static IApplicationBuilder AddLoginModuleApi(this IApplicationBuilder app)
        {            
            return app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
            {                
                endpoints.MapUserManageApi();
            });
        }
    }
}
