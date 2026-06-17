using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Relation;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.Station;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;

public class StationConfigService:IStationConfigService
{
    private readonly MainDbContext _dbContext;

    public StationConfigService(MainDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreated();
    }

    public async Task<IEnumerable<StationConfigTable>?> GetAllStationConfigsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Set<StationConfigTable>().Include(c => c.ToolConfigs).ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
    public Task<StationConfigTable?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();

    }
    public async Task<StationConfigTable?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
    {
        string decodedIp = Uri.UnescapeDataString(stationIp);
        //MyCon1sole.WriteLine($"反转义后的IP为：{decodedIp}");
        return await _dbContext.Set<StationConfigTable>()
            .Include(e => e.ToolConfigs)
            .FirstOrDefaultAsync(e => e.StationIp == decodedIp, cancellationToken);
    }
    public async Task<StationConfigTable?> CreateStationToolConfigAsync(StationConfigTable newStationToolConfig, CancellationToken cancellationToken = default)
    {
        try
        {
            //MyCon1sole.WriteLine($"创建新的图站，IP为：{newToolAgentToolConfig.ToolAgentHostIp}");
            _dbContext.Set<StationConfigTable>().Add(newStationToolConfig);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return newStationToolConfig;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return null;
    }
    public Task<bool> DeleteStationToolConfigAsync(int stationToolConfigId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    public async Task<StationConfigTable?> UpdateStationToolConfigAsync(StationConfigTable updatedStationToolConfig, CancellationToken cancellationToken = default)
    {
        //await _context.Database.EnsureCreatedAsync(cancellationToken);
        var existingStationToolConfig = await _dbContext.Set<StationConfigTable>().FindAsync(new object[] { updatedStationToolConfig.Id }, cancellationToken);
        if (existingStationToolConfig == null)
        {
            Console.WriteLine("find config null,update fail");
            return null;
        }
        var configToDelete = _dbContext.Set<ToolConfig>()
            .Where(itd => itd.StationConfigTableId == existingStationToolConfig.Id)
            .ToList();
        foreach (var config in configToDelete)
        {
            _dbContext.Remove(config);
        }
        // 更新实体的属性
        //var json =JsonSerializer.Serialize(updatedPlug);
        //existingPlug = JsonSerializer.Deserialize<Plug>(json);
        existingStationToolConfig.StationIp = updatedStationToolConfig.StationIp;
        existingStationToolConfig.StationStatus = updatedStationToolConfig.StationStatus;
        existingStationToolConfig.ToolConfigs = updatedStationToolConfig.ToolConfigs;


        _dbContext.Entry(existingStationToolConfig).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existingStationToolConfig;
    }
    public async Task<string?> GetToolPathOnIp(string ip, string toolName, string? version)
    {
        try
        {
            string decodedIp = Uri.UnescapeDataString(ip);
            //MyCon1sole.WriteLine($"查找IP为：{decodedIp}");
            //MyCon1sole.WriteLine($"查找工具为：{toolName}");
            var host = await _dbContext.Set<StationConfigTable>()
                .Include(e => e.ToolConfigs)
                .FirstOrDefaultAsync(e => e.StationIp == decodedIp);
            if (host == null)
            {
                Console.WriteLine($"{decodedIp}（{ip}）未找到该图站的配置表");
                return null;
            }
            //MyCon1sole.WriteLine(host.ToolConfigs?.Count());
            var toolconfigs = host?.ToolConfigs;
            foreach (var t in toolconfigs!)
            {
                var displayName1 = $"{t.ToolName}({t.ToolVersion})";
                var displayName2 = $"{toolName}({version})";
                //MyCon1sole.WriteLine(t.ToolName);
                if (t.ToolName == toolName)
                {
                    //MyCon1sole.WriteLine(t.ToolPath);
                    return t.ToolPath;
                }
            }
            Console.WriteLine($"{decodedIp}（{ip}）未找到工具：{toolName}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message );
            return null;
        }
    }

    public async Task<string?> GetToolPathByFilter(ToolConfigFilter filter)
    {
        try
        {
            //Log.Information(JsonSerializer.Serialize(filter));
            //Log.Information(filter.ToolName);
            //string decodedIp = Uri.UnescapeDataString(filter.StationIP);
            //Log.Information(decodedIp);

            //20250421 修改为通过关系表获取配置
            // 异步查询数据库，获取基础数据
            var dbQuery = _dbContext.Set<CommonRelation>()
                .FirstOrDefault(c =>
                    c.RelationCategory == RelationCategory.ToolToStation.ToString() &&
                    c.RoleAId == filter.ToolId &&
                    c.RoleBId == filter.StationId
                );

            // 异步执行数据库查询，结果加载到内存
            //var results = await dbQuery.ToListAsync();
            //foreach(var result in results)
            //{
            //    Log.Information($"关系表中找到的工具：{result.RoleAName}，图站：{result.RoleBName},配置：{result.RelationSetting}");
            //}
            // 在内存中进一步过滤（同步操作）
            //var relation = results
            //    .Where(c => c.GetRelationSetting("ToolVersion") == filter.ToolVersion)
            //    .FirstOrDefault();
            if (dbQuery == null) 
            {
                //Console.WriteLine($"{decodedIp}（{filter.StationIP}）未找到该图站的配置表，将使用工具默认配置");
                CLog.Information($"未找到该图站({filter.StationIP})的配置表，将使用工具({filter.ToolName}/{filter.ToolVersion})默认配置");
                var toolDefaultPath = await GetToolDefaultPath(filter.ToolId);
                //没有单独配置，使用工具的默认路径,之前已经设置了toolpath，这里没找到就返回空就行
                return toolDefaultPath;
            }            
            var toolDeploySetting = JsonSerializer.Deserialize<ToolDeploySettingModel>(dbQuery.RelationSetting);

            //return relation.GetRelationSetting("ToolPath");
            return toolDeploySetting?.SpecialToolPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            CLog.Error(ex.Message);
            return null;
        }
    }

    public async Task<ToolDeploySettingModel?> GetToolDeploySetting(ToolConfigFilter filter)
    {
        try
        {
            var dbQuery = _dbContext.Set<CommonRelation>()
                .FirstOrDefault(c =>
                    c.RelationCategory == RelationCategory.ToolToStation.ToString() &&
                    c.RoleAId == filter.ToolId &&
                    c.RoleBId == filter.StationId
                );

            if (dbQuery == null)
            {
                return new ToolDeploySettingModel();
            }

            var toolDeploySetting = JsonSerializer.Deserialize<ToolDeploySettingModel>(dbQuery.RelationSetting);
            return toolDeploySetting ?? new ToolDeploySettingModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            CLog.Error(ex.Message);
            return new ToolDeploySettingModel();
        }
    }

    public async Task<string?> GetToolDefaultPath(string? toolName,string? toolVersion)
    {
        if(toolName == null)
        {
            return null;
        }
        var tool = await _dbContext.Set<Tool>().Where(t => t.ToolName == toolName).FirstOrDefaultAsync(t => t.ToolVersion == toolVersion);
        var toolPath = tool?.ToolPath;
        return toolPath;
    }

    private async Task<string?> GetToolDefaultPath(int? toolId)
    {
        if(toolId == null)
        {
            return null;
        }
        var tool = await _dbContext.Set<Tool>().FindAsync(toolId);
        var toolPath = tool?.ToolPath;
        return toolPath;
    }
}

