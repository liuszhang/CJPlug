using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.SystemConfigModel.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.SystemConfigApi.Services;

public class SystemConfigSeedDataProvider : ISeedDataProvider
{
    public string Name => "系统配置模块种子数据";
    public int Order => 130;

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dbContext = serviceProvider.GetRequiredService<MainDbContext>();

        var set = dbContext.Set<SystemConfigItem>();
        var existing = await set.FirstOrDefaultAsync(x => x.Key == RemoteViewModeKey, cancellationToken);
        if (existing != null)
        {
            Console.WriteLine($"[SeedData] SystemConfig {RemoteViewModeKey} 已存在={existing.Value}，跳过");
            return;
        }

        set.Add(new SystemConfigItem
        {
            Key = RemoteViewModeKey,
            Value = "window", // 默认单窗口 RFB VNC 投射，向后兼容
            Description = "远程查看模式：fullscreen=整桌面 VNC，window=单窗口 RFB VNC 投射（默认）",
            UpdatedAt = DateTime.UtcNow.ToLocalTime()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        Console.WriteLine("[SeedData] SystemConfig RemoteViewMode=window 种子数据已写入");
    }

    /// <summary>远程查看模式配置键</summary>
    public const string RemoteViewModeKey = "RemoteViewMode";
}
