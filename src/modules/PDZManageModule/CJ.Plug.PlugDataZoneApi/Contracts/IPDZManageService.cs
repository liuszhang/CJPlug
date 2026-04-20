
using CJ.Plug.Models.PlugProcess;
using System.Text.Json.Nodes;

public interface IPDZManageService
{

    Task<List<PlugDataZone>?> GetAllPdz(CancellationToken cancellationToken = default);
    Task<PlugDataZone?> GetPdzById(int Id,CancellationToken cancellationToken = default);
    Task<PlugDataZone?> GetByPDZId(string PDZId,CancellationToken cancellationToken = default);
    Task<PlugDataZone?> GetByFilter(PDZFilter filter, CancellationToken cancellationToken = default);
    Task<PlugDataZone?> CreatePDZ(PlugDataZone PDZ, CancellationToken cancellationToken = default);
    Task<PlugDataZone?> UpdatePDZ(PlugDataZone PDZ, CancellationToken cancellationToken = default);
    Task<bool> DeletePDZ(string PDZId, CancellationToken cancellationToken = default);
    Task<bool> DeleteByFilter(PDZFilter filter, CancellationToken cancellationToken = default);
}