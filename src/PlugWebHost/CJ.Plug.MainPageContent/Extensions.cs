
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.ApiClient.Services;
using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.Shared;
using CJ.Plug.SharedPages.Contracts;
using CJ.Plug.SharedPages.Services;
using CJ.Plug.VariableUIHandler.Extensions;
using Microsoft.Extensions.DependencyInjection;

public static class CustomElementRegistrationExtensions
{    
    public static IServiceCollection ConfigOtherServices(this IServiceCollection services)
    {
        services.AddSingleton<MainApiClient>();
        //services.AddHttpClient<MainApiClient>(client =>
        //{
        //    //client.BaseAddress = new("https+http://apiservice");
        //    //client.BaseAddress = new("http://localhost:5541");
        //    client.BaseAddress = new(GlobalData.MainDispatcherServer);
        //    client.Timeout = TimeSpan.FromSeconds(20);
        //});
        services.AddHttpClient<ElsaApiClient>(client =>
        {
            client.BaseAddress = new(GlobalData.MainDispatcherServer);
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        //services.Replace(ServiceDescriptor.Scoped<IThemeService, MyThemeService>());

        services.AddSingleton<HubConnectionManagerService>(new HubConnectionManagerService());
        services.AddSingleton<GlobalData>(new GlobalData());

        //添加前端DOM操作服务
        services.AddScoped<IDomInteropService, DomInteropService>();

        services.AddVariableUIHandlers();

        return services;
    }
}
