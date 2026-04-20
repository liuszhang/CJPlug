
using CJ.Plug.ElsaIntegration;
using CJ.Plug.ElsaIntegration.Services;
using CJ.Plug.ElsaIntegration.Pages;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using Elsa.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.BlazorServer.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CJ.Plug.ElsaIntegration.Contracts;
using Elsa.Studio.Agents.UI.Pages;
using Elsa.Workflows.Runtime;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using CJ.Plug.ElsaIntegration.Notifications;

public static class ElsaExtensions
{


    private static IServiceCollection ConfigElsaServices(this IServiceCollection services)
    {
        services.AddElsa(elsa =>
        {
            // Configure Management layer to use EF Core.
            elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite("Data Source=../../main-elsa.db;Cache=Shared;")));

            // Configure Runtime layer to use EF Core.
            elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite("Data Source=../../main-elsa.db;Cache=Shared;")));

            // Default Identity features for authentication/authorization.
            elsa.UseIdentity(identity =>
            {
                identity.TokenOptions = options => options.SigningKey = "sufficiently-large-secret-signing-key"; // This key needs to be at least 256 bits long.
                identity.UseAdminUserProvider();
            });

            // Configure ASP.NET authentication/authorization.
            elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

            // Expose Elsa API endpoints.
            elsa.UseWorkflowsApi();

            // Enable JavaScript workflow expressions.
            elsa.UseJavaScript();

            // Enable C# workflow expressions.
            elsa.UseCSharp();

            // Enable Liquid workflow expressions.
            elsa.UseLiquid();

            // Enable HTTP activities.
            elsa.UseHttp();

            // Use timer activities.
            elsa.UseScheduling();

            elsa.UseWebhooks();
            
            //elsa.RemoveActivity<WriteLine>();

            //注册自定义插头
            //elsa.AddActivity<CommonCorePlugActivity>();


        });

        //services.AddNotificationHandler<WorkflowFinishedHandler>();

        //使用自定义插头提供器动态提供插头
        services.AddActivityProvider<CJActivityProvider>();
        // Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
        services.AddCors(cors => cors
            .AddDefaultPolicy(policy => policy
                .AllowAnyOrigin() // For demo purposes only. Use a specific origin instead.
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("x-elsa-workflow-instance-id"))); // Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.


        return services;
    }

    public static WebApplicationBuilder AddElsaServicesForApi(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigElsaServices();
        
        builder.Services.AddScoped<IElsaEngineService, ElsaEngineService>();

        return builder;
    }

    public static WebApplicationBuilder AddElsaServicesForWeb(this WebApplicationBuilder builder)
    {
        //builder.Services.AddElsaEditorService();
        builder.Services.AddCore();
        //builder.Services.AddElsa();
        //builder.Services.AddShell();
        // Register shell services and modules.
        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => builder.Configuration.GetSection("Backend").Bind(options),
            //ConfigureBackendOptions = options => options.Url=new Uri(GlobalData.ElsaEngineServer),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler)
        };
        builder.Services.AddRemoteBackend(backendApiConfig);
        builder.Services.AddAuthorizationCore();
        builder.Services.AddLoginModuleCore();
        builder.Services.AddLoginModule();
        builder.Services.AddWorkflowsModule();
        builder.Services.AddAgentsModule(backendApiConfig);

        builder.Services.Replace(ServiceDescriptor.Scoped<IThemeService, MyThemeService>());

        builder.Services.AddScoped<IElsaStudioService, ElsaStudioService>();
        builder.Services.AddScoped<IElsaDomToolService, ElsaDomToolService>();

        builder.Services.AddNotificationHandler<PDZUpdatedHandler>();

        return builder;
    }

    public static IApplicationBuilder UseElsaEndpoints(this IApplicationBuilder app)
    {
        app.UseWorkflowsApi(); // Use Elsa API endpoints.
        app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
        app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server. 

        return app;        
    }

    public static IJSComponentConfiguration RegisterCustomElsaStudioElements(this IJSComponentConfiguration configuration)
    {
        //configuration.RegisterCustomElement<ActivityWrapper>("elsa-activity-wrapper");
        configuration.RegisterCustomElement<CustomActivityWrapper>("elsa-activity-wrapper");
        return configuration;
    }
}
