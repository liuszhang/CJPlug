using CJ.Plug.Models.Station;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.StationAndToolApiClient
{
    public interface IStationAndToolApiClient
    {
        Task<Station?> CheckOrCreateStation(string stationIp, CancellationToken cancellationToken = default);
        Task CreateOrSetToolConfig(StationConfigTable stationToolConfig);
        Task<Station?> CreateStationAsync(Station newStation, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> CreateStationToolConfigAsync(StationConfigTable newStationToolConfig, CancellationToken cancellationToken = default);
        Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken cancellationToken = default);
        Task<bool> DeleteStationAsync(int StationId, CancellationToken cancellationToken = default);
        Task<bool> DeleteStationToolConfigAsync(int stationToolConfigId, CancellationToken cancellationToken = default);
        Task<bool> DeleteToolAsync(int? ToolId, CancellationToken cancellationToken = default);
        Task<List<StationConfigTable>?> GetAllStationConfigsAsync(CancellationToken cancellationToken = default);
        Task<List<Station>?> GetAllStationsAsync(CancellationToken cancellationToken = default);
        Task<List<Tool>?> GetAllToolsAsync(CancellationToken cancellationToken = default);
        Task<StationConfigTable?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default);
        Task<Station?> GetStationByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Station?> GetStationByIpAsync(string stationIp, CancellationToken cancellationToken = default);
        Task<string?> GetStationToUse(CancellationToken cancellationToken = default);
        Task<Station?> GetStationToUseByTool(string toolName, string? version = null, CancellationToken cancellationToken = default);
        Task<Tool?> GetToolByDisplayNameAsync(string? toolDisplayName, CancellationToken cancellationToken = default);
        Task<Tool?> GetToolByIdAsync(int? id, CancellationToken cancellationToken = default);
        Task<string?> GetToolPathByFilter(ToolConfigFilter ToolConfigFilter);
        Task<string?> GetToolPathOnIp(string ip, string toolName, string? version = null);
        Task<Station?> UpdateStationAsync(Station updatedStation, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> UpdateStationToolConfigAsync(StationConfigTable updatedStationToolConfig, CancellationToken cancellationToken = default);
        Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken cancellationToken = default);
    }
}
