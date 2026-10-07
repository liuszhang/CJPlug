using CJ.Plug.Models.Shared;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// 2026-10-07 删除：原先这里把配置键 env 覆盖到 ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT。
//   它发生在 CreateBuilder 之后（host 环境已定型）⇒ 对 host 环境无效，却让"配置里写着 Development"
//   看着能控制环境，并会污染子进程环境。环境名一律交给标准来源（launchSettings / 环境变量；缺省 Production）。
//   背景与修复见《CJPlug 发布纳入 CJSuite 与 AppHost 自带 DCP 方案》§9.7。
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production（默认）"}");

//GlobalData.MainApiServer = "http://localhost:5061";
//apisre服务支持
builder.AddServiceDefaults();



// 添加 Swagger 服务
//builder.Services.AddSwaggerGen();

//服务包配置
builder.Services.AddDispatchApiService();


builder.Services.AddEndpointsApiExplorer();
// 添加 NSwag 服务
builder.Services.AddOpenApiDocument(configure =>
{
    configure.Title = "DS API";
});


var app = builder.Build();


app.UseRouting();

app.MapDefaultEndpoints();

//调度API配置
app.UseDispatchServiceEndpoints();


// 启用 NSwag 和 Swagger UI
app.UseOpenApi();
app.UseSwaggerUi();
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();

    //app.UseOpenApi();
    //app.UseSwaggerUi();
    //app.UseOpenApi(settings =>
    //{
    //    settings.Path = "/swagger/ds/swagger.json";
    //});
    //app.UseSwaggerUi(settings =>
    //{
    //    settings.Path = "/swagger/ds";
    //    settings.DocumentPath = "/swagger/ds/swagger.json";
    //});
}

app.Run();
