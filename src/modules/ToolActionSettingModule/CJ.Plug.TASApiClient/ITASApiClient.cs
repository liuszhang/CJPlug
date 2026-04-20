using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.TASApiClient
{
    public interface ITASApiClient
    {
        Task AddPlugActionToPlug(Models.Plug.Plug plug, PlugAction plugAction, RelationTypes relationshipType);
        Task<PlugAction?> CreateAndAddPlugActionToPlug(Models.Plug.Plug plug, PlugAction plugAction, RelationTypes relationshipType);
        Task<Models.Plug.Plug?> CreateNewPlug(Models.Plug.Plug newItem, CancellationToken cancellationToken = default);
        Task<PlugAction?> CreatePlugAction(PlugAction newItem, CancellationToken cancellationToken = default);
        Task<bool> DeletePlug(int? itemId, CancellationToken cancellationToken = default);
        Task<bool> DeletePlugAction(PlugAction? item, CancellationToken cancellationToken = default);
        Task<bool> DeletePlugByDefinitionId(string? definitionId, CancellationToken cancellationToken = default);
        Task DeletePlugToPlugActionRealtionship(PlugToPlugAction plugToPlugAction, CancellationToken cancellationToken = default);
        Task<List<Models.Plug.Plug>?> GetCihldPlugsByDefinitionId(string DefinitionId, CancellationToken cancellationToken = default);
        Task<Models.Plug.Plug?> GetParentPlugById(Models.Plug.Plug ChildPlug);
        Task<PlugAction?> GetPlugActionById(int? plugActionId);
        Task<List<Models.Plug.Plug>?> GetPlugActionsByPlugDefinitionIdAsync(string? plugDefinitionId, CancellationToken cancellationToken = default);
        Task<List<PlugAction>?> GetPlugActionsByPlugId2(int? plugId, CancellationToken cancellationToken = default);
        Task<List<PlugAction>?> GetPlugActionsByPlugIdAsync(int? plugId, CancellationToken cancellationToken = default);
        Task<List<PlugAction>?> GetPlugActionsToExecuteByPlugId(int? plugId, CancellationToken cancellationToken = default);
        Task<Models.Plug.Plug?> GetPlugByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<Models.Plug.Plug?> GetPlugById(int? Id, CancellationToken cancellationToken = default);
        Task<List<Models.Plug.Plug>> GetPlugs(CancellationToken cancellationToken = default);
        Task<string?> GetPlugVariableValueAsync(int plugId, string variableName);
        Task<string?> GetPlugVariableValueAsync(string plugDefinitionId, string variableName);
        Task<Models.Plug.Plug?> GetRootPlugByTypeNameAsync(string typeName, CancellationToken cancellationToken = default);
        Task SetExecutePlugActionsToPlug(Models.Plug.Plug plug, List<PlugAction>? plugActions);
        Task<PlugAction?> UpdatePlugActionAsync(PlugAction newItem, CancellationToken cancellationToken = default);
        Task<Models.Plug.Plug?> UpdatePlugAsync(int? itemId, Models.Plug.Plug item, CancellationToken cancellationToken = default);
    }
}
