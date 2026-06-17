
using CJ.Plug.Models.Station;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public interface IStationManageService
    {
        Task<IEnumerable<Station>?> GetAllStationsAsync(CancellationToken cancellationToken = default);
        Task<Station?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Station?> GetByStationIpAsync(string stationIp, CancellationToken cancellationToken = default);
        Task<Station?> CreateStationAsync(Station newStation, CancellationToken cancellationToken = default);
        Task<bool> DeleteStationAsync(int StationId, CancellationToken cancellationToken = default);
        Task<Station?> UpdateStationAsync(Station updatedStation, CancellationToken cancellationToken = default);



    }

