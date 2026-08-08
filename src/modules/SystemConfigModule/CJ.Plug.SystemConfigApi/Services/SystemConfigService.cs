using CJ.Plug.Models.Shared;
using CJ.Plug.SystemConfigApi.Contracts;
using CJ.Plug.SystemConfigModel.Models;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.SystemConfigApi.Services;

public class SystemConfigService(MainDbContext dbContext) : ISystemConfigService
{
    public async Task<List<SystemConfigItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.Set<SystemConfigItem>()
            .OrderBy(x => x.Key)
            .ToListAsync(ct);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var item = await dbContext.Set<SystemConfigItem>()
            .FirstOrDefaultAsync(x => x.Key == key, ct);
        return item?.Value;
    }

    public async Task<SystemConfigItem?> SetValueAsync(string key, string value, string? description = null, CancellationToken ct = default)
    {
        var item = await dbContext.Set<SystemConfigItem>()
            .FirstOrDefaultAsync(x => x.Key == key, ct);
        if (item == null)
        {
            item = new SystemConfigItem { Key = key, Value = value, Description = description, UpdatedAt = DateTime.UtcNow.ToLocalTime() };
            dbContext.Set<SystemConfigItem>().Add(item);
        }
        else
        {
            item.Value = value;
            if (!string.IsNullOrEmpty(description)) item.Description = description;
            item.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        }
        await dbContext.SaveChangesAsync(ct);
        return item;
    }
}
