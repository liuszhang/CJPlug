using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Station;
using Microsoft.EntityFrameworkCore;
using Serilog;

public class ToolManageService : IToolManageService
{
    private readonly MainDbContext _dbContext;

    public ToolManageService(MainDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task<IEnumerable<Tool>?> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Set<Tool>().ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
    public async Task<Tool?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Set<Tool>().FindAsync(new object[] { id }, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }

    }
    public async Task<Tool?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
    {
        string decodedIp = Uri.UnescapeDataString(stationIp);
        //MyCon1sole.WriteLine($"反转义后的IP为：{decodedIp}");
        return await _dbContext.Set<Tool>()
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken cancellationToken = default)
    {
        try
        {
            //MyCon1sole.WriteLine($"创建新的图站，IP为：{newToolAgentToolConfig.ToolAgentHostIp}");
            _dbContext.Set<Tool>().Add(newTool);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return newTool;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return null;
    }
    public async Task<bool> DeleteToolAsync(int ToolId, CancellationToken cancellationToken = default)
    {
        var tool = await _dbContext.Set<Tool>().FindAsync(ToolId);
        if (tool == null)
        {
            return false;
        }

        _dbContext.Set<Tool>().Remove(tool);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    public async Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken cancellationToken = default)
    {
        //await _context.Database.EnsureCreatedAsync(cancellationToken);
        var existingTool = await _dbContext.Set<Tool>().FindAsync(new object[] { updatedTool.Id }, cancellationToken);
        if (existingTool == null)
        {
            await CreateToolAsync(updatedTool);
            return updatedTool;
        }
        // 更新实体的属性
        existingTool.ToolName = updatedTool.ToolName;
        existingTool.ToolVersion = updatedTool.ToolVersion;
        existingTool.ToolType = updatedTool.ToolType;
        existingTool.ToolPath = updatedTool.ToolPath;
        existingTool.ToolCompany= updatedTool.ToolCompany;
        existingTool.CommandParameter = updatedTool.CommandParameter;
        existingTool.ToolDescription = updatedTool.ToolDescription;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existingTool;
    }
    public async Task<Tool?> GetByDisplayNameAsync(string ToolDisplayName, CancellationToken cancellationToken = default)
    {
        var toolName = ToolDisplayName.Split("(")[0];
        var toolVersion = ToolDisplayName.Split("(")[1].TrimEnd(')');
        try
        {
            return await _dbContext.Set<Tool>()
                .Where(t => t.ToolName == toolName).FirstOrDefaultAsync(t => t.ToolVersion == toolVersion,cancellationToken);
        }
        catch (Exception ex)
        {
            CLog.Error(ex.ToString());
            return null;
        }
    }
}

