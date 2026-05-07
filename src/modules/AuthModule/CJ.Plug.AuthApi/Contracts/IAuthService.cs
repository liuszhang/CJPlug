using CJ.Plug.AuthModels;

namespace CJ.Plug.AuthApi.Contracts
{
    public interface IAuthService
    {
        Task<List<AuthRequestDto>> GetAllAsync(AuthRequestStatus? status = null, CancellationToken cancellationToken = default);
        Task<AuthRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<AuthRequestDto> CreateAsync(CreateAuthRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthRequestDto?> ApproveAsync(ApproveAuthRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthRequestDto?> CancelAsync(int id, string cancelledBy, CancellationToken cancellationToken = default);
        Task<bool> HasPendingRequestAsync(AuthOperationType operationType, string targetDescription, CancellationToken cancellationToken = default);
    }
}
