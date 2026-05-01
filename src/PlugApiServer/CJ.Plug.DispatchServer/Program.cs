using CJ.Plug.Models.Shared;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", configuration.GetValue<string>("env"));
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", configuration.GetValue<string>("env"));
Console.WriteLine($"当前环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

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
