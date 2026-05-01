using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using StlViewerPlug.Services;

namespace StlViewerPlug.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStlViewer(this IServiceCollection services)
    {
        services
            .AddScoped<IPlugCommonSettingContent, StlViewerPlugCommonSettingContent>()
            .AddScoped<IPlugCommonExecute, StlViewerPlugCommonExecuteService>();
        return services;
    }

    public static IServiceCollection AddStlViewerExecute(this IServiceCollection services)
    {
        services
            .AddScoped<IPlugCommonExecute, StlViewerPlugCommonExecuteService>();
        return services;
    }
}
