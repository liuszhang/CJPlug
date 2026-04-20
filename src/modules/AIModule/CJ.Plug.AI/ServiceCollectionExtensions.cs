using CJ.Plug.AI.Pages;
using CJ.Plug.DeekSeekIn;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIModuleServices(this IServiceCollection services)
    {
        services.AddScoped<IModule, AIModule>();
        services.AddScoped<IDeepSeekService, DeepSeekService>();
        services.AddAIChatServices();

        return services;
    }


    public class AIModule(IAppBarService appBarService) : ModuleBase
    {
        public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            appBarService.AddAppBarItem<AskAI>();
            //Console.WriteLine("Add Ai APPBAR Item.");
            return base.InitializeAsync(cancellationToken);
        }
    }

}

