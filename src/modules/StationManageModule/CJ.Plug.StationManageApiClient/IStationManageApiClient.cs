using System.IO;
using CJ.Plug.Models.Station;

namespace CJ.Plug.StationManageApiClient
{
    public interface IStationManageApiClient
    {
        // Station CRUD
        Task<List<Station>?> GetAllStationsAsync(CancellationToken cancellationToken = default);
        Task<Station?> GetStationByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Station?> GetStationByIpAsync(string stationIp, CancellationToken cancellationToken = default);
        Task<Station?> CreateStationAsync(Station newStation, CancellationToken cancellationToken = default);
        Task<bool> DeleteStationAsync(int StationId, CancellationToken cancellationToken = default);
        Task<Station?> UpdateStationAsync(Station updatedStation, CancellationToken cancellationToken = default);
        Task<Station?> CheckOrCreateStation(string stationIp, CancellationToken cancellationToken = default);
        Task<string?> GetStationToUse(CancellationToken cancellationToken = default);
        Task<Station?> GetStationToUseByTool(string toolName, string? version = null, string? specifiedStationIp = null, CancellationToken cancellationToken = default);

        // StationConfig CRUD
        Task<List<StationConfigTable>?> GetAllStationConfigsAsync(CancellationToken cancellationToken = default);
        Task<StationConfigTable?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> CreateStationToolConfigAsync(StationConfigTable newStationToolConfig, CancellationToken cancellationToken = default);
        Task CreateOrSetToolConfig(StationConfigTable stationToolConfig);
        Task<bool> DeleteStationToolConfigAsync(int stationToolConfigId, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> UpdateStationToolConfigAsync(StationConfigTable updatedStationToolConfig, CancellationToken cancellationToken = default);
        Task<string?> GetToolPathOnIp(string ip, string toolName, string? version = null);
        Task<string?> GetToolPathByFilter(ToolConfigFilter ToolConfigFilter);
        Task<ToolDeploySettingModel?> GetToolDeploySettingAsync(ToolConfigFilter filter);
    }
}
