using CJ.Plug.VariableUIHandler.Contracts;
using CJ.Plug.VariableUIHandler.Services;
using CJ.Plug.VariableUIHandler.VariableUIHandlers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVariableUIHandlers(this IServiceCollection services)
        {
            services.AddScoped<IVariableUIService, VariableUIService>();

            return services
                .AddVariableUIHandler<FileTypeHandler>()      
                .AddVariableUIHandler<TextMappingTypeHandler>()
                .AddVariableUIHandler<ModelParameterTypeHandler>()
                .AddVariableUIHandler<RequestHeaderTypeHandler>()
                .AddVariableUIHandler<DefaultOutputMappingTypeHandler>()
                .AddVariableUIHandler<DefaultInputMappingTypeHandler>()
                .AddVariableUIHandler<ToolCommandVariableTypeUIHandler>()
                .AddVariableUIHandler<ToolTypeHandler>()
                .AddVariableUIHandler<WordTextMappingTypeHandler>()
                .AddVariableUIHandler<ConditionExpressionTypeHandler>()
                ;
        }


        /// <summary>
        /// Adds the specified <see cref="IVariableUIHandler"/>.
        /// </summary>
        public static IServiceCollection AddVariableUIHandler<T>(this IServiceCollection services) where T : class, IVariableUIHandler
        {
            return services.AddScoped<IVariableUIHandler, T>();
        }
    }
}
