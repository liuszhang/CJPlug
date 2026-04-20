
using CJ.Plug.Models.Station;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public interface IStationConfigService
    {
        Task<IEnumerable<StationConfigTable>?> GetAllStationConfigsAsync(CancellationToken cancellationToken = default);
        Task<StationConfigTable?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> CreateStationToolConfigAsync(StationConfigTable newStationToolConfig, CancellationToken cancellationToken = default);
        Task<bool> DeleteStationToolConfigAsync(int stationToolConfigId, CancellationToken cancellationToken = default);
        Task<StationConfigTable?> UpdateStationToolConfigAsync(StationConfigTable updatedStationToolConfig, CancellationToken cancellationToken = default);


        Task<string?> GetToolPathOnIp(string ip, string toolName, string? version);
        Task<string?> GetToolPathByFilter(ToolConfigFilter filter);

    }

