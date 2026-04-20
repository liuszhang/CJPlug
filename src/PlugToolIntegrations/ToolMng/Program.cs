using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.OpenApi.Models;
using ToolMng;
using ToolMng.Contracts;
using ToolMng.Models;
using ToolMng.Services;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
StaticData.MainServerHostIp = configuration.GetSection("MainServer").GetSection("Url").Value;
Console.WriteLine("连接到主机地址："+StaticData.MainServerHostIp);
//StaticData.ToolAgentServerHttpsPort = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Https").GetSection("Url").Value.Split(':')[2];
StaticData.ToolAgentServerHttpScheme = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Http").GetSection("Url").Value.Split(':')[0];
StaticData.ToolAgentServerHttpPort = configuration.GetSection("Kestrel").GetSection("Endpoints").GetSection("Http").GetSection("Url").Value.Split(':')[2];
StaticData.ToolAgentServer = configuration.GetSection("FileServer").GetSection("ToolAgentServer").Value.ToString();
// Add services to the container.



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: "AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins(StaticData.MainServerHostIp) // 允许的前端应用地址
                   .AllowAnyMethod() // 允许任何HTTP方法
                   .AllowAnyHeader(); // 允许任何头部
        });
    options.AddPolicy(
            name: "AllowAll Origins",
            builder =>
            {
                builder.SetIsOriginAllowed(_ => true) // 允许所有来源
                       .AllowAnyMethod() // 允许任何HTTP方法
                       .AllowAnyHeader(); // 允许任何头部
            });
});

builder.Services.AddScoped<IToolAgentServices, ToolAgentServices>();
builder.Services.AddScoped<IToolAgentNXServices, ToolAgentNXServices>();

builder.Services.AddHostedService<ToolMngService>(); // 注册为后台服务

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// 使用CORS策略
app.UseCors("AllowAll Origins");

app.UseRouting();

//app.UseEndpoints(endpoints => endpoints.MapHub<ToolAgentStatusHub>("/toolAgentStatusHub"));

app.MapControllers();

app.Run();
