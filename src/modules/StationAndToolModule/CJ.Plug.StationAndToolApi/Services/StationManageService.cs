using CJ.Plug.Models.Station;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Serilog;

public class StationManageService:IStationManageService
{
    private readonly MainDbContext _dbContext;
    private HubConnectionManagerService HubConnectionManagerService { get; set; }

    public StationManageService(MainDbContext dbContext, HubConnectionManagerService hubConnectionManagerService)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreatedAsync();
        HubConnectionManagerService = hubConnectionManagerService;
        HubConnectionManagerService._hubConnection.Remove("StatusInfo");
        HubConnectionManagerService._hubConnection.On<string, string>("StatusInfo", (ip, status) =>
        {
            Console.WriteLine($"Receive:{ip},Status:{status}");
            // 更新图站状态
            var station = _dbContext.Set<Station>().FirstOrDefault(s => s.StationIp == ip);
            if (station != null)
            {
                station.StationStatus = status;
                _dbContext.SaveChanges();
                Log.Information($"图站 {ip} 状态 Updated 为 {status}");
            }
            else
            {
                // 如果图站不存在，可以选择创建新的图站或忽略
                _dbContext.Set<Station>().Add(new Station
                {
                    StationIp = ip,
                    StationName = ip,
                    StationStatus = status,
                    IsStarted = false,
                    UpdateTime = DateTime.Now.ToString(),
                    StationCategory = "Unknown",
                });
                _dbContext.SaveChanges();
                Log.Information($"图站 {ip} 不存在，已 Create 新图站，状态为 {status}");
            }
        });
    }

    public async Task<IEnumerable<Station>?> GetAllStationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Set<Station>().ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
    public Task<Station?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();

    }
    public async Task<Station?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default)
    {
        string decodedIp = Uri.UnescapeDataString(stationIp);
        //Log.Information($"反转义后的IP为：{decodedIp}");
        return await _dbContext.Set<Station>()
            .FirstOrDefaultAsync(e => e.StationIp == decodedIp.TrimEnd('/'), cancellationToken);
    }
    public async Task<Station?> CreateStationAsync(Station newStation, CancellationToken cancellationToken = default)
    {
        try
        {
            //MyCon1sole.WriteLine($"创建新的图站，IP为：{newToolAgentToolConfig.ToolAgentHostIp}");
            _dbContext.Set<Station>().Add(newStation);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return newStation;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return null;
    }
    public async Task<bool> DeleteStationAsync(int StationId, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Station>().Remove(new Station { Id = StationId });
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;

    }
    public async Task<Station?> UpdateStationAsync(Station updatedStation, CancellationToken cancellationToken = default)
    {
        //await _context.Database.EnsureCreatedAsync(cancellationToken);
        var existingStation = await _dbContext.Set<Station>().FindAsync(new object[] { updatedStation.Id }, cancellationToken);
        if (existingStation == null)
        {
            await CreateStationAsync(updatedStation);
            return updatedStation;
        }
        // 更新实体的属性
        //var json =JsonSerializer.Serialize(updatedPlug);
        //existingPlug = JsonSerializer.Deserialize<Plug>(json);
        existingStation.StationIp = updatedStation.StationIp;
        existingStation.StationName = updatedStation.StationName;
        existingStation.StationStatus = updatedStation.StationStatus;
        existingStation.IsStarted = updatedStation.IsStarted;
        existingStation.UpdateTime = updatedStation.UpdateTime;


        _dbContext.Entry(existingStation).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existingStation;
    }
}

