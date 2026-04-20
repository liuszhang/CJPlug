using CJ.Plug.Models.Shared;
using CJ.Plug.StationAgent.Contracts;
using CJ.Plug.StationAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.StationAgent.Shared
{
    public static class ServiceConfig
    {
        public static IServiceCollection AddStationApiClient(this IServiceCollection services)
        {
            services.AddHttpClient<StationApiClient>(client =>
            {
                client.BaseAddress = new("http://localhost:7660");
                //client.Timeout = TimeSpan.FromSeconds(60);
            });

            //services.AddScoped<IClientApiService, ClientApiService>();

            return services;
        }
    }
}
