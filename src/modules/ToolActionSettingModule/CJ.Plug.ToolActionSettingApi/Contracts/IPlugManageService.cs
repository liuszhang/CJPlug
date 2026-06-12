using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;

public interface IPlugManageService
{
    Task<Plug> CreatePlugAsync(Plug request, CancellationToken cancellationToken = default);
    Task<bool> DeletePlugAsync(int id);
    Task<IEnumerable<Plug>> GetAllPlugsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Plug>?> GetChildPlugsAsync(string ProcessDefinitionId, CancellationToken cancellationToken = default);
    Task<Plug?> GetPlugById(int id);
    Task<Plug?> GetParentPlugById(int? id);
    Task<Plug?> GetPlugByDefinitionId(string definiitonId);
    Task<Plug?> GetPlugByTypeName(string typeName);
    Task<Plug?> GetPlugByNameAsync(string name);
    Task<Plug?> UpdatePlugAsync(int id, Plug request);
    Task<string?> GetExecuteString(Plug toolItem, PlugAction toolActionItem);


    Task<IEnumerable<PlugAction>> GetAllPlugActionsAsync(CancellationToken cancellationToken = default);
    Task<PlugAction> CreatePlugActionAsync(PlugAction request, CancellationToken cancellationToken = default);
    Task<PlugAction?> UpdatePlugActionAsync(PlugAction request);
    Task<bool> DeletePlugActionAsync(int id);
    Task<IEnumerable<PlugAction>?> GetPlugActionsByPlugIdAsync(int? Id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Plug>?> GetPlugActionsByPlugDefinitionIdAsync(string? DefinitionId, CancellationToken cancellationToken = default);
    Task<PlugAction?> GetPlugActionByIdAsync(int? Id, CancellationToken cancellationToken = default);



    Task<List<PlugVariable>?> GetVariablesByDefinitionId(string definiitonId);


    Task<IEnumerable<PlugToPlugAction>> GetAllRealationssAsync(CancellationToken cancellationToken = default);
    Task<PlugToPlugAction> CreateRealationAsync(PlugToPlugAction request, CancellationToken cancellationToken = default);
    Task<bool> DeleteRealationsAsync(PlugToPlugAction request, CancellationToken cancellationToken = default);
    Task<bool> DeleteRealationsAsync(int? plugId, string? plugActionDefinitionId);


    
}