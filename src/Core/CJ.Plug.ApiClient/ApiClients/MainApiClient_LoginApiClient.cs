//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;
using CJ.Plug.Models.PlugProcess;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.ApiClient.Contracts;
using Microsoft.AspNetCore.Components;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.Models.Relation;
using Microsoft.Extensions.DependencyInjection;
using CJ.Plug.FileManageApiClient;
using CJ.Plug.JobManageApiClient;
using CJ.Plug.LoginApiClient.ApiClients;
using CJ.Plug.TASApiClient;
using CJ.Plug.StationAndToolApiClient;
using CJ.Plug.ProcessManageApiClient;

public partial class MainApiClient : ILoginApiClient
{
    public Task<User?> Login(User user, CancellationToken cancellationToken = default)=>LoginApiClient.Value.Login(user, cancellationToken);

    public Task Logout(string userId, CancellationToken cancellationToken = default)=>LoginApiClient.Value.Logout(userId, cancellationToken);

    public Task<User?> Register(User user, CancellationToken cancellationToken = default)=>LoginApiClient.Value.Register(user, cancellationToken);
}




