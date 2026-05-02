using Microsoft.Extensions.DependencyInjection;

namespace ThreeJsIntegration.Extensions;

/// <summary>
/// ThreeJsIntegration 服务注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 ThreeJsIntegration 服务。Razor 组件库不需要额外注册服务，
    /// 此方法作为扩展点预留，供将来配置使用。
    /// </summary>
    public static IServiceCollection AddThreeJsIntegration(this IServiceCollection services)
    {
        // Razor 组件库的静态资源由框架自动处理。
        // 此处预留给将来可能的配置选项（如自定义 Draco 解码器路径等）。
        return services;
    }
}
