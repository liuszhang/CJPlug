
using System.IO;
using System.Text.Json;
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
using Elsa.Studio.Login.Contracts;
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
using Elsa.Identity.Options;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using CJ.Plug.ElsaIntegration.Notifications;
using CJ.Plug.ElsaIntegration.ApiClient;

public static class ElsaExtensions
{


    private static IServiceCollection ConfigElsaServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // 优先从 IConfiguration 读取（与 ApiServer 一致），失败时回退到文件扫描
        var elsaConnectionString = TryReadFromConfiguration(configuration, "ConnectionStrings:ElsaDb")
                                   ?? ReadElsaConnectionString();
        var elsaDbType = TryReadFromConfiguration(configuration, "DatabaseConfig:DbType")
                         ?? ReadElsaDbType();

        Console.WriteLine($"[ElsaExtensions] DbType={elsaDbType}, ConnStr prefix={elsaConnectionString?.Substring(0, Math.Min(elsaConnectionString?.Length ?? 0, 30))}...");

        services.AddElsa(elsa =>
        {
            // Configure Management layer to use EF Core.
            elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef =>
            {
                switch (elsaDbType)
                {
                    case "PostgreSQL": ef.UsePostgreSql(elsaConnectionString); break;
                    case "SqlServer": ef.UseSqlServer(elsaConnectionString); break;
                    default: ef.UseSqlite(elsaConnectionString); break;
                }
            }));

            // Configure Runtime layer to use EF Core.
            elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef =>
            {
                switch (elsaDbType)
                {
                    case "PostgreSQL": ef.UsePostgreSql(elsaConnectionString); break;
                    case "SqlServer": ef.UseSqlServer(elsaConnectionString); break;
                    default: ef.UseSqlite(elsaConnectionString); break;
                }
            }));

            // Default Identity features for authentication/authorization.
            elsa.UseIdentity(identity =>
            {
                // ⚠ 勿改回硬编码默认值：Elsa.Identity 的 TokenOptions 校验要求「已知公开默认值只在 Development/Demo 允许」，
                //    硬编码会让安装态/非 VS 启动直接以 OptionsValidationException: SigningKey uses a known public
                //    default value 起不来（2026-10-07 实机踩到）。详见方案 §9.7 与 ElsaSigningKeyResolver。
                identity.TokenOptions = options => options.SigningKey = ElsaSigningKeyResolver.Resolve(configuration);

                // ⚠ Elsa 3.9 起「内置管理员用户 / 内置管理员 API Key / localhost 自动放行」三条引导路径全部改为
                //    「未显式配置 = 全拒」，未引导的实例每个管理端点都回 401/403。实机症状：流程编辑器「组件库」
                //    空白，日志刷出 RemoteActivityRegistryProvider 的 Refit 401 堆栈。
                //
                // 这里刻意**不用** identity.UseDefaultAdmin(...)（虽然它才是 Elsa 自检推荐的第一选项）：它挂上的
                //    AdminUserInitializer 会与下面 AdminUserProvider 自带的 AdminRoleProvider 抢同一个 'admin' 角色，
                //    启动时必抛（2026-10-08 实测）：
                //      "Background task AdminUserInitializer failed with an error
                //       System.InvalidOperationException: A role with ID 'admin' already exists."
                //    两者叠加的净效果只有一条错误日志 + 一个永远用不到的 store 用户。
                //
                // 因此只把 DefaultAdminUserOptions 的值填上（不安装 DefaultAdminUserFeature ⇒ 没有 initializer ⇒
                //    不会抢角色）：Elsa 的 IdentityBootstrapDiagnostic 据此认定"本实例已引导"，不再每次启动刷
                //      "No users exist and no identity bootstrap is configured … answer 403" 的红字。
                services.Configure<DefaultAdminUserOptions>(options =>
                {
                    options.AdminUserName = ElsaAdminCredentialResolver.ResolveUserName(configuration);
                    options.AdminPassword = ElsaAdminCredentialResolver.ResolvePassword(configuration);
                    options.AdminRoleName = ElsaAdminCredentialResolver.AdminRoleName;
                    options.AdminRolePermissions = ElsaAdminCredentialResolver.AdminRolePermissions;
                });

                // 真正的凭据通道：AdminUserProvider 在**请求时**校验，不依赖任何启动任务，因此把身份引导从
                //    "Elsa tenant 启动任务链"上彻底解耦（2026-10-08 冷启动实测洞 F4：链上前序任务抛异常会让
                //    后续的 AdminUserInitializer 不再执行 ⇒ 用户种不上 ⇒ 登录恒 false 且不自愈）。
                //    它同时把 RoleProvider 换成 AdminRoleProvider（无条件给 '*'），所以即便 store 里没有任何
                //    角色/用户，令牌权限也是满的。两处凭据同源（同一个 ElsaAdminCredentialResolver），不可能不一致。
                //    代价：IUserProvider 变为 AdminUserProvider ⇒ MemoryUserStore 里的其它 Elsa 用户不可见（当前无其它用户）。
                identity.UseAdminUserProvider(options =>
                {
                    options.UserName = ElsaAdminCredentialResolver.ResolveUserName(configuration);
                    options.Password = ElsaAdminCredentialResolver.ResolvePassword(configuration);
                });
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
            elsa.UseLiquid();

            // Enable HTTP activities.
            //elsa.UseHttp();

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

    private static string? FindElsaApiServerConfigPath()
    {
        var dir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "src")) &&
                Directory.Exists(Path.Combine(dir, "02.Publish")))
            {
                // 优先 src 源码目录（VS 调试时 builder.Configuration 从此读取）
                var srcPath = Path.Combine(dir, "src", "PlugApiServer", "CJ.Plug.ElsaApiServer", "appsettings.json");
                if (File.Exists(srcPath)) return srcPath;

                // 回退到 02.Publish 构建输出目录（Debug/Release 自动适配）
                var debugPath = Path.Combine(dir, "02.Publish", "CJ.Plug.ElsaApiServer", "Debug", "net10.0", "appsettings.json");
                if (File.Exists(debugPath)) return debugPath;

                var releasePath = Path.Combine(dir, "02.Publish", "CJ.Plug.ElsaApiServer", "Release", "net10.0", "appsettings.json");
                if (File.Exists(releasePath)) return releasePath;

                return null;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    private static string? TryReadFromConfiguration(IConfiguration? configuration, string key)
    {
        if (configuration == null) return null;
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string ReadElsaConnectionString()
    {
        try
        {
            var configPath = FindElsaApiServerConfigPath();
            Console.WriteLine($"[ElsaExtensions] ReadElsaConnectionString: configPath={configPath ?? "(null)"}");
            if (configPath == null || !File.Exists(configPath))
            {
                Console.WriteLine("[ElsaExtensions] Config file not found, using default SQLite connection string");
                return "Data Source=../../main-elsa.db;Cache=Shared;";
            }

            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStr) &&
                connStr.TryGetProperty("ElsaDb", out var elsaDb))
            {
                var val = elsaDb.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    Console.WriteLine($"[ElsaExtensions] ReadElsaConnectionString OK, prefix={val.Substring(0, Math.Min(val.Length, 50))}...");
                    return val;
                }
            }
            Console.WriteLine("[ElsaExtensions] ConnectionStrings:ElsaDb not found or empty in config file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ElsaExtensions] ReadElsaConnectionString failed: {ex.Message}");
        }

        return "Data Source=../../main-elsa.db;Cache=Shared;";
    }

    private static string ReadElsaDbType()
    {
        try
        {
            var configPath = FindElsaApiServerConfigPath();
            Console.WriteLine($"[ElsaExtensions] ReadElsaDbType: configPath={configPath ?? "(null)"}");
            if (configPath == null || !File.Exists(configPath))
            {
                Console.WriteLine("[ElsaExtensions] Config file not found, defaulting to SQLite");
                return "SQLite";
            }

            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("DatabaseConfig", out var dbConfig) &&
                dbConfig.TryGetProperty("DbType", out var dbTypeElem))
            {
                var val = dbTypeElem.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    Console.WriteLine($"[ElsaExtensions] ReadElsaDbType OK, value={val}");
                    return val;
                }
            }
            Console.WriteLine("[ElsaExtensions] DatabaseConfig:DbType not found or empty in config file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ElsaExtensions] ReadElsaDbType failed: {ex.Message}");
        }

        return "SQLite";
    }

    public static WebApplicationBuilder AddElsaServicesForApi(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigElsaServices(builder.Configuration);
        
        builder.Services.AddScoped<IElsaEngineService, ElsaEngineService>();

        builder.Services.AddScoped<IElsaApiClient, ElsaApiClient>();

        return builder;
    }

    public static IServiceCollection ConfigElsaServicesWithOutDB(this IServiceCollection services)
    {
        services.AddElsa(elsa =>
        {            
            // Default Identity features for authentication/authorization.
            elsa.UseIdentity(identity =>
            {
                identity.TokenOptions = options => options.SigningKey = ElsaSigningKeyResolver.Resolve();
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
            elsa.UseLiquid();

            // Enable HTTP activities.
            //elsa.UseHttp();

            // Use timer activities.
            elsa.UseScheduling();

            elsa.UseWebhooks();

            //elsa.RemoveActivity<WriteLine>();

            //注册自定义插头
            //elsa.AddActivity<CommonCorePlugActivity>();


        });

        services.AddScoped<IElsaEngineService, ElsaEngineService>();

        services.AddScoped<IElsaApiClient, ElsaApiClient>();
        return services;
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

        // Studio 的 HTTP / SignalR / 登录态三处统一从 IJwtAccessor 取令牌，而 CJ 的流程编辑器内嵌在自己的
        // 页面里、从不显示 Studio 登录页 ⇒ 默认实现（读写浏览器 localStorage）里永远没有令牌 ⇒ 401。
        // 替换为「内存缓存 + 惰性自动登录」实现：令牌由服务端直连 ElsaApiServer 的 /identity/login 取回。
        // ⚠ 必须在 AddLoginModule()（注册 BlazorServerJwtAccessor）之后，否则会被后注册者覆盖回去。
        // 用「同程序集工厂注册」而不是 ServiceDescriptor.Scoped<TService,TImpl>()：后者要让 DI 反射构造
        // internal 实现类型，跨程序集可见性上是隐患；工厂 lambda 在本程序集内 new，零歧义。
        builder.Services.Replace(ServiceDescriptor.Scoped<IJwtAccessor>(sp => new CjElsaAutoLoginJwtAccessor(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<IRemoteBackendAccessor>(),
            sp.GetRequiredService<IConfiguration>())));

        builder.Services.UseElsaIdentity();
        builder.Services.AddWorkflowsModule();
        builder.Services.AddAgentsModule(backendApiConfig);

        builder.Services.Replace(ServiceDescriptor.Scoped<IThemeService, MyThemeService>());

        builder.Services.AddScoped<IElsaStudioService, ElsaStudioService>();
        builder.Services.AddScoped<IElsaDomToolService, ElsaDomToolService>();

        builder.Services.AddNotificationHandler<PDZUpdatedHandler>();

        builder.Services.AddScoped<IElsaApiClient, ElsaApiClient>();

        return builder;
    }

    public static IApplicationBuilder UseElsaEndpoints(this IApplicationBuilder app)
    {
        app.UseWorkflowsApi(); // Use Elsa API endpoints.
        //app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
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
