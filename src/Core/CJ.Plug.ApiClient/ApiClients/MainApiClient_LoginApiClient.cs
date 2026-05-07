using CJ.Plug.AuditModels;
using CJ.Plug.Models.Shared;
using CJ.Plug.LoginApiClient.ApiClients;

public partial class MainApiClient : ILoginApiClient
{
    public async Task<User?> Login(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await LoginApiClient.Value.Login(user, cancellationToken);
            if (result != null)
                await AuditLog.LogSuccessAsync(AuditModule.System, AuditOperationType.Login, $"用户登录: {user.UserName}");
            else
                await AuditLog.LogFailureAsync(AuditModule.System, AuditOperationType.Login, $"用户登录失败: {user.UserName}", "用户名或密码错误");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.System, AuditOperationType.Login, $"用户登录异常: {user.UserName}", ex.Message);
            throw;
        }
    }

    public async Task Logout(string userId, CancellationToken cancellationToken = default)
    {
        await LoginApiClient.Value.Logout(userId, cancellationToken);
        await AuditLog.LogSuccessAsync(AuditModule.System, AuditOperationType.Logout, $"用户登出: {userId}");
    }

    public async Task<User?> Register(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await LoginApiClient.Value.Register(user, cancellationToken);
            if (result != null)
                await AuditLog.LogSuccessAsync(AuditModule.UserManage, AuditOperationType.Create, $"用户注册: {user.UserName}");
            else
                await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Create, $"用户注册失败: {user.UserName}", "注册失败");
            return result;
        }
        catch (Exception ex)
        {
            await AuditLog.LogFailureAsync(AuditModule.UserManage, AuditOperationType.Create, $"用户注册异常: {user.UserName}", ex.Message);
            throw;
        }
    }
}
