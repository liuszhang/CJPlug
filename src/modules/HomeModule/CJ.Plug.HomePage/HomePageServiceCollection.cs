using CJ.Plug.HomePage.Menus;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CJ.Plug.HomePage
{
    public static class HomePageServiceCollection
    {
        public static IServiceCollection AddHomePageModuleServices(this IServiceCollection services)
        {
            
            services.AddScoped<IModule, HomePageModule>();
            services.AddScoped<IMenuService, HomePageMenu>();

            return services;
        }

        public class HomePageModule : ModuleBase
        {
        }
    }
}