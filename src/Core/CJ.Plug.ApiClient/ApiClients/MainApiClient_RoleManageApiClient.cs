using CJ.Plug.UserManageApiClient;
using CJ.Plug.UserManageModels;

public partial class MainApiClient : IRoleManageApiClient
{
    Task<List<RoleManageDto>> IRoleManageApiClient.GetAllAsync(CancellationToken cancellationToken)
        => RoleManageApiClient.Value.GetAllAsync(cancellationToken);

    Task<RoleManageDto?> IRoleManageApiClient.GetByIdAsync(int id, CancellationToken cancellationToken)
        => RoleManageApiClient.Value.GetByIdAsync(id, cancellationToken);

    Task<RoleManageDto?> IRoleManageApiClient.CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
        => RoleManageApiClient.Value.CreateAsync(request, cancellationToken);

    Task<RoleManageDto?> IRoleManageApiClient.UpdateAsync(UpdateRoleRequest request, CancellationToken cancellationToken)
        => RoleManageApiClient.Value.UpdateAsync(request, cancellationToken);

    Task<bool> IRoleManageApiClient.DeleteAsync(int id, CancellationToken cancellationToken)
        => RoleManageApiClient.Value.DeleteAsync(id, cancellationToken);
}
