//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.AuthApiClient;
using CJ.Plug.DeekSeekIn;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.GuacamoleApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.LoginApiClient.ApiClients;
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
using CJ.Plug.ToolResourceApiClient;
using CJ.Plug.TASApiClient;
using CJ.Plug.UserManageApiClient;
using CJ.Plug.UserManageModels;
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
    public readonly Lazy<ILoginApiClient> LoginApiClient;
    public readonly Lazy<IExecuteApiClient> ExecuteApiClient;
    public readonly Lazy<ITASApiClient> TASApiClient;
    public readonly Lazy<IFileManageApiClient> FileManageApiClient;
    public readonly Lazy<IJobManageApiClient> JobManageApiClient;
    public readonly Lazy<IStationManageApiClient> StationManageApiClient;
    public readonly Lazy<IToolResourceApiClient> ToolResourceApiClient;
    public readonly Lazy<IPlugMarketApiClient> PlugMarketApiClient;
    public readonly Lazy<IProcessManageApiClient> ProcessManageApiClient;
    public readonly Lazy<IUserManageApiClient> UserManageApiClient;
    public readonly Lazy<IRoleManageApiClient> RoleManageApiClient;
    public readonly Lazy<IMCPToolApiClient> MCPToolApiClient;
    public readonly Lazy<IDepartmentManageApiClient> DepartmentManageApiClient;
    public readonly Lazy<IAuthApiClient> AuthApiClient;
    public readonly Lazy<IGuacamoleApiClient> GuacamoleApiClient;
    public readonly Lazy<IRolePermissionApiClient> RolePermissionApiClient;
    public readonly Lazy<IGroupManageApiClient> GroupManageApiClient;
    public readonly Lazy<ISkillApiClient> SkillApiClient;
    //public readonly Lazy<IDeepSeekService> DeepSeekApiClient;

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
        LoginApiClient = new Lazy<ILoginApiClient>(() => serviceProvider.GetRequiredService<ILoginApiClient>());
        ExecuteApiClient = new Lazy<IExecuteApiClient>(() => serviceProvider.GetRequiredService<IExecuteApiClient>());
        TASApiClient = new Lazy<ITASApiClient>(() => serviceProvider.GetRequiredService<ITASApiClient>());
        FileManageApiClient = new Lazy<IFileManageApiClient>(() => serviceProvider.GetRequiredService<IFileManageApiClient>());
        JobManageApiClient = new Lazy<IJobManageApiClient>(() => serviceProvider.GetRequiredService<IJobManageApiClient>());
        StationManageApiClient = new Lazy<IStationManageApiClient>(() => serviceProvider.GetRequiredService<IStationManageApiClient>());
        ToolResourceApiClient = new Lazy<IToolResourceApiClient>(() => serviceProvider.GetRequiredService<IToolResourceApiClient>());
        PlugMarketApiClient = new Lazy<IPlugMarketApiClient>(() => serviceProvider.GetRequiredService<IPlugMarketApiClient>());
        ProcessManageApiClient = new Lazy<IProcessManageApiClient>(() => serviceProvider.GetRequiredService<IProcessManageApiClient>());
        UserManageApiClient = new Lazy<IUserManageApiClient>(() => serviceProvider.GetRequiredService<IUserManageApiClient>());
        RoleManageApiClient = new Lazy<IRoleManageApiClient>(() => serviceProvider.GetRequiredService<IRoleManageApiClient>());
        MCPToolApiClient = new Lazy<IMCPToolApiClient>(() => serviceProvider.GetRequiredService<IMCPToolApiClient>());
        DepartmentManageApiClient = new Lazy<IDepartmentManageApiClient>(() => serviceProvider.GetRequiredService<IDepartmentManageApiClient>());
        AuthApiClient = new Lazy<IAuthApiClient>(() => serviceProvider.GetRequiredService<IAuthApiClient>());
        GuacamoleApiClient = new Lazy<IGuacamoleApiClient>(() => serviceProvider.GetRequiredService<IGuacamoleApiClient>());
        RolePermissionApiClient = new Lazy<IRolePermissionApiClient>(() => serviceProvider.GetRequiredService<IRolePermissionApiClient>());
        GroupManageApiClient = new Lazy<IGroupManageApiClient>(() => serviceProvider.GetRequiredService<IGroupManageApiClient>());
        SkillApiClient = new Lazy<ISkillApiClient>(() => serviceProvider.GetRequiredService<ISkillApiClient>());
        //DeepSeekApiClient = new Lazy<IDeepSeekService>(() => serviceProvider.GetRequiredService<IDeepSeekService>());

        // 初始化审计日志辅助类
        var auditHttpClient = new HttpClient { BaseAddress = new Uri(GlobalData.MainApiServer) };
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
