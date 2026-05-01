//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.DeekSeekIn;
using CJ.Plug.FileManageApiClient;
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
using CJ.Plug.StationAndToolApiClient;
using CJ.Plug.TASApiClient;
using CJ.Plug.UserManageApiClient;
using CJ.Plug.UserManageModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Net.Http.Json;
using System.Text.Json;

public partial class MainApiClient
{
    private readonly IServiceProvider serviceProvider;

    private readonly Lazy<IRelationApiClient> RelationApiClient;
    private readonly Lazy<IPDZApiClient> PDZApiClient;
    private readonly Lazy<ILoginApiClient> LoginApiClient;
    private readonly Lazy<IExecuteApiClient> ExecuteApiClient;
    private readonly Lazy<ITASApiClient> TASApiClient;
    private readonly Lazy<IFileManageApiClient> FileManageApiClient;
    private readonly Lazy<IJobManageApiClient> JobManageApiClient;
    private readonly Lazy<IStationAndToolApiClient> StationAndToolApiClient;
    private readonly Lazy<IPlugMarketApiClient> PlugMarketApiClient;
    private readonly Lazy<IProcessManageApiClient> ProcessManageApiClient;
    private readonly Lazy<IUserManageApiClient> UserManageApiClient;
    private readonly Lazy<IRoleManageApiClient> RoleManageApiClient;
    private readonly Lazy<IMCPToolApiClient> MCPToolApiClient;
    //private readonly Lazy<IDeepSeekService> DeepSeekApiClient;

    public MainApiClient(IServiceProvider _serviceProvider)
    {
        // 锟斤拷式锟斤拷锟斤拷锟斤拷锟角凤拷为 null锟斤拷锟斤拷前锟斤拷锟斤拷锟斤拷锟斤拷确锟斤拷示
        serviceProvider = _serviceProvider ?? throw new ArgumentNullException(
            nameof(serviceProvider),
            "IServiceProvider 锟斤拷锟斤拷注锟诫，锟斤拷锟斤拷为 null锟斤拷锟斤拷锟斤拷锟斤拷锟斤拷注锟斤拷锟斤拷锟矫★拷"
        );
        this.serviceProvider = _serviceProvider;

        RelationApiClient = new Lazy<IRelationApiClient>(() => serviceProvider.GetRequiredService<IRelationApiClient>());
        PDZApiClient = new Lazy<IPDZApiClient>(() => serviceProvider.GetRequiredService<IPDZApiClient>());
        LoginApiClient = new Lazy<ILoginApiClient>(() => serviceProvider.GetRequiredService<ILoginApiClient>());
        ExecuteApiClient = new Lazy<IExecuteApiClient>(() => serviceProvider.GetRequiredService<IExecuteApiClient>());
        TASApiClient = new Lazy<ITASApiClient>(() => serviceProvider.GetRequiredService<ITASApiClient>());
        FileManageApiClient = new Lazy<IFileManageApiClient>(() => serviceProvider.GetRequiredService<IFileManageApiClient>());
        JobManageApiClient = new Lazy<IJobManageApiClient>(() => serviceProvider.GetRequiredService<IJobManageApiClient>());
        StationAndToolApiClient = new Lazy<IStationAndToolApiClient>(() => serviceProvider.GetRequiredService<IStationAndToolApiClient>());
        PlugMarketApiClient = new Lazy<IPlugMarketApiClient>(() => serviceProvider.GetRequiredService<IPlugMarketApiClient>());
        ProcessManageApiClient = new Lazy<IProcessManageApiClient>(() => serviceProvider.GetRequiredService<IProcessManageApiClient>());
        UserManageApiClient = new Lazy<IUserManageApiClient>(() => serviceProvider.GetRequiredService<IUserManageApiClient>());
        RoleManageApiClient = new Lazy<IRoleManageApiClient>(() => serviceProvider.GetRequiredService<IRoleManageApiClient>());
        MCPToolApiClient = new Lazy<IMCPToolApiClient>(() => serviceProvider.GetRequiredService<IMCPToolApiClient>());
        //DeepSeekApiClient = new Lazy<IDeepSeekService>(() => serviceProvider.GetRequiredService<IDeepSeekService>());
    }

}




