using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Login.BlazorServer.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Localization.BlazorServer;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Localization.BlazorServer.Extensions;
using Elsa.Studio.Translations;

// Build the host.
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var configuration = builder.Configuration;

// Register Razor services.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Register the root components.
    options.RootComponents.RegisterCustomElsaStudioElements();
});

// Register shell services and modules.
builder.Services.AddCore();
builder.Services.AddShell(options => configuration.GetSection("Shell").Bind(options));

// Register shell services and modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => builder.Configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler)
};

builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddAuthorizationCore();
builder.Services.AddLoginModule();
builder.Services.AddLoginModuleCore();
builder.Services.UseElsaIdentity();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddAgentsModule(backendApiConfig);
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

builder.Services.AddLocalization();
// Register the Localization Module
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddTranslations();

// If using your own, register your locallization provider
//builder.Services.AddSingleton<ILocalizationProvider, MyLocalizationProvider>();

// Configure SignalR.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize to handle large workflows.
    options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
});

// Build the application.
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseResponseCompression();

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseElsaLocalization();
app.MapControllers();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Run the application.
app.Run();