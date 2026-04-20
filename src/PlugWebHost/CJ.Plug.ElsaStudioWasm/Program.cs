
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.BlazorWasm;
using Elsa.Studio.Localization.BlazorWasm.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register root components.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Register shell services and modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => builder.Configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler)
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddLoginModule();
builder.Services.UseElsaIdentity();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
// Define the Localization Configuration
var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options =>
    {
        configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options);
        options.SupportedCultures = new[] { options?.DefaultCulture ?? new LocalizationOptions().DefaultCulture }
            .Concat(options?.SupportedCultures.Where(culture => culture != options?.DefaultCulture) ?? []).ToArray();
    }
};

// Register the Localization Module
builder.Services.AddLocalizationModule(localizationConfig);

// If using your own, register your locallization provider
//builder.Services.AddSingleton<ILocalizationProvider, MyLocalizationProvider>();




// Build the application.
var app = builder.Build();

// Run each startup task.
var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

// Use the localization Middleware
await app.UseElsaLocalization();

// Run the application.
await app.RunAsync();