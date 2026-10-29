//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.DeekSeekIn;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.GuacamoleApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.MCPToolApiClient;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Relation;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.ProcessManageApiClient;
using CJ.Plug.SkillApiClient;
using CJ.Plug.StationManageApiClient;
using CJ.Plug.SystemConfigApiClient;
using CJ.Plug.ToolResourceApiClient;
using CJ.Plug.TASApiClient;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Net.Http.Json;
using System.Text.Json;

public partial class MainApiClient
{
    private readonly IServiceProvider serviceProvider;

    public readonly Lazy<IRelationApiClient> RelationApiClient;
    public readonly Lazy<IPDZApiClient> PDZApiClient;
    public readonly Lazy<IExecuteApiClient> ExecuteApiClient;
    public readonly Lazy<ITASApiClient> TASApiClient;
    public readonly Lazy<IFileManageApiClient> FileManageApiClient;
    public readonly Lazy<IJobManageApiClient> JobManageApiClient;
    public readonly Lazy<IStationManageApiClient> StationManageApiClient;
    public readonly Lazy<IToolResourceApiClient> ToolResourceApiClient;
    public readonly Lazy<IPlugMarketApiClient> PlugMarketApiClient;
    public readonly Lazy<IProcessManageApiClient> ProcessManageApiClient;
    public readonly Lazy<IMCPToolApiClient> MCPToolApiClient;
    public readonly Lazy<IGuacamoleApiClient> GuacamoleApiClient;
    public readonly Lazy<ISkillApiClient> SkillApiClient;
    public readonly Lazy<ISystemConfigApiClient> SystemConfigApiClient;
    //public readonly Lazy<IDeepSeekService> DeepSeekApiClient;

    // P5.6+：旧身份体系（登录/用户/角色/部门/用户组/授权请求/角色权限）已整体下线，
    // 对应 7 个 ApiClient 属性一并移除 —— 三员模块（CJCore.Modules.ThreeRoleAdmin）接管。
    // 若将来需要旧体系，从 git 历史恢复本文件对应行 + 恢复 src/modules/{Login,UserManage,Auth}Module。

    /// <summary>
    /// 审计日志辅助类
    /// </summary>
    public AuditLogHelper AuditLog { get; private set; }

    public MainApiClient(IServiceProvider _serviceProvider)
    {
        serviceProvider = _serviceProvider ?? throw new ArgumentNullException(
            nameof(serviceProvider),
            "IServiceProvider 不能为 null，请检查依赖注入配置"
        );

        RelationApiClient = new Lazy<IRelationApiClient>(() => serviceProvider.GetRequiredService<IRelationApiClient>());
        PDZApiClient = new Lazy<IPDZApiClient>(() => serviceProvider.GetRequiredService<IPDZApiClient>());
        ExecuteApiClient = new Lazy<IExecuteApiClient>(() => serviceProvider.GetRequiredService<IExecuteApiClient>());
        TASApiClient = new Lazy<ITASApiClient>(() => serviceProvider.GetRequiredService<ITASApiClient>());
        FileManageApiClient = new Lazy<IFileManageApiClient>(() => serviceProvider.GetRequiredService<IFileManageApiClient>());
        JobManageApiClient = new Lazy<IJobManageApiClient>(() => serviceProvider.GetRequiredService<IJobManageApiClient>());
        StationManageApiClient = new Lazy<IStationManageApiClient>(() => serviceProvider.GetRequiredService<IStationManageApiClient>());
        ToolResourceApiClient = new Lazy<IToolResourceApiClient>(() => serviceProvider.GetRequiredService<IToolResourceApiClient>());
        PlugMarketApiClient = new Lazy<IPlugMarketApiClient>(() => serviceProvider.GetRequiredService<IPlugMarketApiClient>());
        ProcessManageApiClient = new Lazy<IProcessManageApiClient>(() => serviceProvider.GetRequiredService<IProcessManageApiClient>());
        MCPToolApiClient = new Lazy<IMCPToolApiClient>(() => serviceProvider.GetRequiredService<IMCPToolApiClient>());
        GuacamoleApiClient = new Lazy<IGuacamoleApiClient>(() => serviceProvider.GetRequiredService<IGuacamoleApiClient>());
        SkillApiClient = new Lazy<ISkillApiClient>(() => serviceProvider.GetRequiredService<ISkillApiClient>());
        SystemConfigApiClient = new Lazy<ISystemConfigApiClient>(() => serviceProvider.GetRequiredService<ISystemConfigApiClient>());
        SystemConfigApiClient = new Lazy<ISystemConfigApiClient>(() => serviceProvider.GetRequiredService<ISystemConfigApiClient>());
        //DeepSeekApiClient = new Lazy<IDeepSeekService>(() => serviceProvider.GetRequiredService<IDeepSeekService>());

        // 初始化审计日志辅助类
        // ⚠ P5.8 C2：这是**绕过 IHttpClientFactory 的裸实例**，必须用 InternalApiKey.CreateClient()
        //   才能带上内部密钥。否则 /api/audit 一旦收口，全仓 299 处 AuditLogHelper 调用立刻 401 刷屏。
        var auditHttpClient = InternalApiKey.CreateClient();
        auditHttpClient.BaseAddress = new Uri(GlobalData.MainApiServer);
        AuditLog = new AuditLogHelper(auditHttpClient, GetUserNameAsync);
    }

    /// <summary>
    /// 从localStorage获取当前用户名（通过IServiceProvider获取Scoped服务）
    /// </summary>
    private async Task<string> GetUserNameAsync()
    {
        try
        {
            // 使用IServiceProvider创建scope来获取Scoped服务
            using var scope = serviceProvider.CreateScope();
            var localStorage = scope.ServiceProvider.GetService<ILocalStorageService>();
            //var localStorage = serviceProvider.GetRequiredService<ILocalStorageService>();
            if (localStorage != null)
            {
                return await localStorage.GetItemAsync<string>("username") ?? "anonymous";
            }
            return "anonymous";
        }
        catch
        {
            return "anonymous";
        }
    }
}
