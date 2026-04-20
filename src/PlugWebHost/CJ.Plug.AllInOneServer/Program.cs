using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using PlugsBundle;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.Services.AddSignalR();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Sink(new SignalRLogSink("API"))
    .CreateLogger();


builder.Services.AddElsa(elsa =>
{
    // Configure Management layer to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite()));

    // Configure Runtime layer to use EF Core.
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite()));

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
    //elsa.UseJavaScript();

    // Enable C# workflow expressions.
    elsa.UseCSharp();

    // Enable Liquid workflow expressions.
    //elsa.UseLiquid();

    // Enable HTTP activities.
    elsa.UseHttp();

    // Use timer activities.
    elsa.UseScheduling();

    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();

    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();
});

// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin() // For demo purposes only. Use a specific origin instead.
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("x-elsa-workflow-instance-id"))); // Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.


builder.Services.AddSingleton<HubConnectionManagerService>(new HubConnectionManagerService());

//builder.Services.AddMainApiService();

builder.Services.AddPlugsExecutebundle();
builder.Services.AddHttpClient<MainApiClient>(client =>
{
    client.BaseAddress = new("http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(60);
});


// Add Health Checks.
builder.Services.AddHealthChecks();

// Build the web application.
var app = builder.Build();
// 确保 UseRouting 在其他使用端点的中间件之前调用
app.UseRouting();
// Configure web application's middleware pipeline.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
//app.UseMainEndpointsManage();

app.Run();