using CJ.Plug.WorkflowCoreIntegration.Contracts;
using CJ.Plug.WorkflowCoreIntegration.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.WorkflowCoreIntegration
{
    public static class Extensions
    {
        public static IServiceCollection AddWorkflowCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IWorkflowCoreService, WorkflowCoreService>();

            return services;
        }
    }
}
