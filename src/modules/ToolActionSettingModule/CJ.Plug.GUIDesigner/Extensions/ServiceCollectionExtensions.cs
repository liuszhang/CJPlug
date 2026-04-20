//using FastEndpoints;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.GUIDesigner.Contracts;
using CJ.Plug.GUIDesigner.Services;
using Microsoft.Extensions.DependencyInjection;



public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGuiDesignerService(this IServiceCollection services)
        {

        services.AddScoped<IGuiItemHandlerService, GuiItemHandlerService>();
        services.AddScoped<IGuiItemService, BaseGuiItemService>();
        services.AddScoped<IGuiItemService, RichTextGuiItemService>();

        
        return services;
        }

}

