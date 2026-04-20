using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Relation;

public interface IRelationManageService : IBaseRepositoryService<CommonRelation, int>
{
    Task<IEnumerable<CommonRelation?>> GetAllRelationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CommonRelation?>> GetRelationsByCategoryAsync(string Category,CancellationToken cancellationToken = default);

    Task<CommonRelation?> CreateRealationAsync(CommonRelation request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CommonRelation?>> GetRealationByFilterAsync(RelationFilter filter, CancellationToken cancellationToken = default);
    Task<bool> DeleteRealationAsync(CommonRelation request, CancellationToken cancellationToken = default);
    Task<CommonRelation?> UpdateRealationAsync(CommonRelation request, CancellationToken cancellationToken = default);
}

