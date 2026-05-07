using CJ.Plug.AuditModels;
using CJ.Plug.Models.Relation;

public partial class MainApiClient : IRelationApiClient
{
    public async Task<CommonRelation?> CreateOrUpdateRelationAsync(CommonRelation request, CancellationToken cancellationToken = default)
    {
        var result = await RelationApiClient.Value.CreateOrUpdateRelationAsync(request, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Update, $"创建或更新关系ID: {request.Id}");
        return result;
    }

    public async Task<bool> DeleteRealationAsync(CommonRelation request, CancellationToken cancellationToken = default)
    {
        var result = await RelationApiClient.Value.DeleteRealationAsync(request, cancellationToken);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Delete, $"删除关系ID: {request.Id}");
        return result;
    }

    public async Task<List<CommonRelation?>> GetAllRelations(CancellationToken cancellationToken = default)
    {
        var result = await RelationApiClient.Value.GetAllRelations(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "查询所有关系");
        return result;
    }

    public async Task<List<CommonRelation>?> GetRealationByFilterAsync(RelationFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await RelationApiClient.Value.GetRealationByFilterAsync(filter, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, "按筛选查询关系");
        return result;
    }

    public async Task<List<CommonRelation?>> GetRelationsByCategoryAsync(string Category, CancellationToken cancellationToken = default)
    {
        var result = await RelationApiClient.Value.GetRelationsByCategoryAsync(Category, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.Other, AuditOperationType.Other, $"按分类查询关系: {Category}");
        return result;
    }
}
