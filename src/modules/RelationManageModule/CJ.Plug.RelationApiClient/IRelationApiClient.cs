using CJ.Plug.Models.Relation;

public interface IRelationApiClient
{
    Task<CommonRelation?> CreateOrUpdateRelationAsync(CommonRelation request, CancellationToken cancellationToken = default);
    Task<bool> DeleteRealationAsync(CommonRelation request, CancellationToken cancellationToken = default);
    Task<List<CommonRelation?>> GetAllRelations(CancellationToken cancellationToken = default);
    Task<List<CommonRelation>?> GetRealationByFilterAsync(RelationFilter filter, CancellationToken cancellationToken = default);
    Task<List<CommonRelation?>> GetRelationsByCategoryAsync(string Category, CancellationToken cancellationToken = default);
}

