using CJ.Plug.Models.Shared;

namespace CJ.Plug.LoginApis.Contracts
{
    public interface ILoginService
    {
        Task<User?> RegisterUserAsync(User dto);
        Task<User?> LoginUserAsync(User dto);
        Task LogoutUserAsync(string userId);
        //Task<bool> VerifyEmailAsync(string userId);
        //Task ResetPasswordAsync(ResetPasswordDto dto);
        //Task<IEnumerable<User>?> GetAllUsers();
    }
}
