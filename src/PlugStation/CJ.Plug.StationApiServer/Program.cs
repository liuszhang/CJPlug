using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Login;
using CJ.Plug.Models.Contracts;
using CJ.Plug.ModuleConfig;
using CJ.Plug.StationApiServer.Contracts;
using CJ.Plug.StationApiService.Contracts;
using CJ.Plug.StationApiService.Services;
using CJ.Plug_Aspire.StationApiService.Models;
using CJ.Plug_Aspire.StationApiService.Services;
using CJ.Plug_Aspire.StationApiService.StationApi;
using Microsoft.AspNetCore.Builder;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", configuration.GetValue<string>("env"));
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", configuration.GetValue<string>("env"));
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

Log.Logger = new LoggerConfiguration()
    //.WriteTo.File("StationLogs/log.txt",
    //    rollingInterval: RollingInterval.Day,
    //    rollOnFileSizeLimit: true)
    .WriteTo.Sink(new SignalRLogSink("Station"))
    .CreateLogger();

StaticData.MainServerHostIp = configuration.GetSection("MainServer").GetSection("Url").Value;
Console.WriteLine("the main serverIp is:" + StaticData.MainServerHostIp);
//StaticData.ToolAgentServerHttpsPort = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Https").GetSection("Url").Value.Split(':')[2];
StaticData.ToolAgentServerHttpScheme = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Http").GetSection("Url").Value.Split(':')[0];
//StaticData.ToolAgentServerHttpPort = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Http").GetSection("Url").Value.Split(':')[2];
StaticData.ToolAgentServer = configuration.GetSection("FileServer").GetSection("ToolAgentServer").Value.ToString();





builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddOpenApiDocument(configure =>
{
    configure.Title = "Station API";
});

builder.Services.AddHostedService<StationHubService>(); // 注册为后台服务


builder.Services.AddScoped<IStationExecuteService, DefaultStationExecuteService>();
builder.Services.AddScoped<IStationFileService, StationFileService>();

builder.Services.ConfigModuleApiServices();


builder.Services.AddSingleton<MainApiClient>();
//builder.Services.AddHttpClient<MainApiClient>(client =>
//{
//    //client.BaseAddress = new("https+http://apiservice");
//    client.BaseAddress = new(StaticData.MainServerHostIp);
//    client.Timeout = TimeSpan.FromSeconds(60);
//});


var app = builder.Build();

app.MapDefaultEndpoints();

app.UseOpenApi();
app.UseSwaggerUi();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
    Console.WriteLine("---Development Mode---");
    //app.UseOpenApi();
    //app.UseSwaggerUi();
    //app.UseOpenApi(settings =>
    //{
    //    settings.Path = "/swagger/station/swagger.json";
    //});
    //app.UseSwaggerUi(settings =>
    //{
    //    settings.Path = "/swagger/station";
    //    settings.DocumentPath = "/swagger/station/swagger.json";
    //});
}

//app.UseHttpsRedirection();


app.MapConnectionApi();

app.Run();
