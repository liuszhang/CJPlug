using CJ.Plug.AuditModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using CJ.Plug.TASApiClient;

public partial class MainApiClient : ITASApiClient
{
    public async Task AddPlugActionToPlug(Plug plug, PlugAction plugAction, RelationTypes relationshipType)
    {
        await TASApiClient.Value.AddPlugActionToPlug(plug, plugAction, relationshipType);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Update, $"添加插件动作到插件: {plug.Name}");
    }

    public async Task<PlugAction?> CreateAndAddPlugActionToPlug(Plug plug, PlugAction plugAction, RelationTypes relationshipType)
    {
        var result = await TASApiClient.Value.CreateAndAddPlugActionToPlug(plug, plugAction, relationshipType);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Create, $"创建并添加插件动作: {plugAction.Name}");
        return result;
    }

    public async Task<Plug?> CreateNewPlug(Plug newItem, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.CreateNewPlug(newItem, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Create, $"创建插件: {newItem.Name}");
        return result;
    }

    public async Task<PlugAction?> CreatePlugAction(PlugAction newItem, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.CreatePlugAction(newItem, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Create, $"创建插件动作: {newItem.Name}");
        return result;
    }

    public async Task<bool> DeletePlug(int? itemId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.DeletePlug(itemId, cancellationToken);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Delete, $"删除插件ID: {itemId}");
        return result;
    }

    public async Task<bool> DeletePlugAction(PlugAction? item, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.DeletePlugAction(item, cancellationToken);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Delete, $"删除插件动作: {item?.Name}");
        return result;
    }

    public async Task<bool> DeletePlugByDefinitionId(string? definitionId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.DeletePlugByDefinitionId(definitionId, cancellationToken);
        if (result)
            await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Delete, $"删除插件定义ID: {definitionId}");
        return result;
    }

    public async Task DeletePlugToPlugActionRealtionship(PlugToPlugAction plugToPlugAction, CancellationToken cancellationToken = default)
    {
        await TASApiClient.Value.DeletePlugToPlugActionRealtionship(plugToPlugAction, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Delete, "删除插件动作关系");
    }

    public async Task<List<Plug>?> GetCihldPlugsByDefinitionId(string DefinitionId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetCihldPlugsByDefinitionId(DefinitionId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询子插件定义ID: {DefinitionId}");
        return result;
    }

    public async Task<Plug?> GetParentPlugById(Plug ChildPlug)
    {
        var result = await TASApiClient.Value.GetParentPlugById(ChildPlug);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询父插件: {ChildPlug.Name}");
        return result;
    }

    public async Task<PlugAction?> GetPlugActionById(int? plugActionId)
    {
        var result = await TASApiClient.Value.GetPlugActionById(plugActionId);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询插件动作ID: {plugActionId}");
        return result;
    }

    public async Task<List<Plug>?> GetPlugActionsByPlugDefinitionIdAsync(string? plugDefinitionId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetPlugActionsByPlugDefinitionIdAsync(plugDefinitionId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询插件动作定义ID: {plugDefinitionId}");
        return result;
    }

    public async Task<List<PlugAction>?> GetPlugActionsByPlugId2(int? plugId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetPlugActionsByPlugId2(plugId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询插件动作插件ID: {plugId}");
        return result;
    }

    public async Task<List<PlugAction>?> GetPlugActionsByPlugIdAsync(int? plugId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetPlugActionsByPlugIdAsync(plugId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询插件动作插件ID: {plugId}");
        return result;
    }

    public async Task<List<PlugAction>?> GetPlugActionsToExecuteByPlugId(int? plugId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetPlugActionsToExecuteByPlugId(plugId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询待执行插件动作: {plugId}");
        return result;
    }

    public async Task<Plug?> GetPlugByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetPlugByDefinitionIdAsync(definitionId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"通过定义ID查询插头: {definitionId}");
        return result;
    }

    public async Task<Plug?> GetPlugById(int? Id, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetPlugById(Id, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"通过ID查询插头: {Id}");
        return result;
    }

    public async Task<List<Plug>> GetPlugs(CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetPlugs(cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, "查询所有插头");
        return result;
    }

    public async Task<string?> GetPlugVariableValueAsync(int plugId, string variableName)
    {
        var result = await TASApiClient.Value.GetPlugVariableValueAsync(plugId, variableName);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"获取插头变量值: {plugId}.{variableName}");
        return result;
    }

    public async Task<string?> GetPlugVariableValueAsync(string plugDefinitionId, string variableName)
    {
        var result = await TASApiClient.Value.GetPlugVariableValueAsync(plugDefinitionId, variableName);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"获取插头变量值: {plugDefinitionId}.{variableName}");
        return result;
    }

    public async Task<Plug?> GetRootPlugByTypeNameAsync(string typeName, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.GetRootPlugByTypeNameAsync(typeName, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Other, $"查询根插头类型: {typeName}");
        return result;
    }

    public async Task SetExecutePlugActionsToPlug(Plug plug, List<PlugAction>? plugActions)
    {
        await TASApiClient.Value.SetExecutePlugActionsToPlug(plug, plugActions);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Update, $"设置执行插件动作: {plug.Name}");
    }

    public async Task<PlugAction?> UpdatePlugActionAsync(PlugAction newItem, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.UpdatePlugActionAsync(newItem, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Update, $"更新插件动作: {newItem.Name}");
        return result;
    }

    public async Task<Plug?> UpdatePlugAsync(int? itemId, Plug item, CancellationToken cancellationToken = default)
    {
        var result = await TASApiClient.Value.UpdatePlugAsync(itemId, item, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Update, $"更新插件: {item.Name}");
        return result;
    }

    public async Task BatchUpdateSortOrdersAsync(List<PlugSortOrderDto> sortOrders, CancellationToken cancellationToken = default)
    {
        await TASApiClient.Value.BatchUpdateSortOrdersAsync(sortOrders, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.PlugManage, AuditOperationType.Update, $"批量更新插头排序: {sortOrders.Count} 项");
    }
}
