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

//public partial class MainApiClient : IDeepSeekService
//{

//    public IAsyncEnumerable<string> Ask(string Question) => DeepSeekApiClient.Value.Ask(Question);
//    public IAsyncEnumerable<string> AskWithTool(string Question) => DeepSeekApiClient.Value.AskWithTool(Question);
//}




