using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CJ.Plug.ElsaIntegration;

/// <summary>
/// Elsa 管理员引导凭据解析器（与 <see cref="ElsaSigningKeyResolver"/> 同款风格：可覆盖、可自愈、绝不落"已知默认值"）。
///
/// <para>
/// <b>为什么需要它</b>：Elsa 3.9 起「内置管理员用户 / 内置管理员 API Key / localhost 自动放行」三条引导路径
/// 全部改成**必须显式配置**——未引导的实例每个管理端点都回 401/403。实机症状是流程编辑器「组件库」空白，
/// 并在日志里刷出 <c>RemoteActivityRegistryProvider.ListAsync</c> 的 Refit 401 堆栈。
/// </para>
///
/// <para>
/// <b>取值优先级</b>：① 环境变量 → ② 配置键 → ③ <b>由每机签名密钥确定性派生</b>。
/// 第 ③ 档是关键：签名密钥已由 <see cref="ElsaSigningKeyResolver"/> 保证"每机唯一 + 多服务收敛"，
/// 口令是它的**纯函数**，因此服务端（种管理员）与客户端（登录）两侧**构造性一致**，
/// 不存在第二个收敛点、也不需要新增任何密钥文件（组装脚本黑名单无需变更）。
/// </para>
///
/// <para>
/// <b>域分隔</b>：派生时前置固定标签 <c>CJPlug.Elsa.Admin|</c>，与签名用途分开，避免同一串字节两用。
/// </para>
///
/// <para>
/// <b>为什么不打印口令</b>：只打印命中哪一档来源。口令本身既可能来自运维注入，也可能是每机派生值。
/// </para>
/// </summary>
internal static class ElsaAdminCredentialResolver
{
    /// <summary>用户名环境变量。</summary>
    internal const string UserEnvVarName = "CJPLUG_ELSA_ADMIN_USER";

    /// <summary>口令环境变量。</summary>
    internal const string PasswordEnvVarName = "CJPLUG_ELSA_ADMIN_PASSWORD";

    /// <summary>用户名配置键（appsettings 里可写 <c>"Elsa": { "AdminUser": "..." }</c>）。</summary>
    internal const string UserConfigKey = "Elsa:AdminUser";

    /// <summary>口令配置键（appsettings 里可写 <c>"Elsa": { "AdminPassword": "..." }</c>）。</summary>
    internal const string PasswordConfigKey = "Elsa:AdminPassword";

    /// <summary>默认管理员用户名。</summary>
    internal const string DefaultUserName = "admin";

    /// <summary>管理角色名（与 Elsa <c>DefaultAdminUserOptions.AdminRoleName</c> 的默认值一致）。</summary>
    internal const string AdminRoleName = "admin";

    /// <summary>派生口令时的域标签（改它等于换掉全机的管理员口令）。</summary>
    private const string DerivationLabel = "CJPlug.Elsa.Admin|";

    private static readonly object Gate = new();
    private static string? _cachedUserName;
    private static string? _cachedPassword;

    /// <summary>
    /// 管理角色权限。<see cref="Elsa.Workflows.Activities"/> 侧用 <c>"*"</c> 表示整库通配（resource wildcard），
    /// 与 Elsa <c>DefaultAdminUserOptions.AdminRolePermissions</c> 的默认值一致。
    /// 每次返回新实例，避免把可变集合暴露成共享静态状态。
    /// </summary>
    internal static ICollection<string> AdminRolePermissions => new[] { "*" };

    /// <summary>解析（并缓存）管理员用户名；<paramref name="configuration"/> 可为 null（配置档位自动跳过）。</summary>
    internal static string ResolveUserName(IConfiguration? configuration = null)
    {
        if (_cachedUserName is { Length: > 0 })
            return _cachedUserName;

        lock (Gate)
        {
            if (_cachedUserName is { Length: > 0 })
                return _cachedUserName;

            _cachedUserName =
                TryEnvVar(UserEnvVarName, "管理员用户名") ??
                TryConfiguration(configuration, UserConfigKey, "管理员用户名");

            if (string.IsNullOrWhiteSpace(_cachedUserName))
            {
                _cachedUserName = DefaultUserName;
                Console.WriteLine($"[Elsa] 管理员用户名：{DefaultUserName}（默认，可用 {UserEnvVarName} 或配置键 {UserConfigKey} 覆盖）");
            }

            return _cachedUserName!;
        }
    }

    /// <summary>解析（并缓存）管理员口令；<paramref name="configuration"/> 可为 null（配置档位自动跳过）。</summary>
    internal static string ResolvePassword(IConfiguration? configuration = null)
    {
        if (_cachedPassword is { Length: > 0 })
            return _cachedPassword;

        lock (Gate)
        {
            if (_cachedPassword is { Length: > 0 })
                return _cachedPassword;

            _cachedPassword =
                TryEnvVar(PasswordEnvVarName, "管理员口令") ??
                TryConfiguration(configuration, PasswordConfigKey, "管理员口令");

            if (string.IsNullOrWhiteSpace(_cachedPassword))
            {
                _cachedPassword = DeriveFromSigningKey(configuration);
                Console.WriteLine($"[Elsa] 管理员口令：由签名密钥派生（域标签 {DerivationLabel}，每机唯一；不落盘、不打印；可用 {PasswordEnvVarName} 或配置键 {PasswordConfigKey} 覆盖）");
            }

            return _cachedPassword!;
        }
    }

    private static string? TryEnvVar(string envVarName, string label)
    {
        var value = Environment.GetEnvironmentVariable(envVarName);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        Console.WriteLine($"[Elsa] {label}来源：环境变量 {envVarName}");
        return value.Trim();
    }

    private static string? TryConfiguration(IConfiguration? configuration, string configKey, string label)
    {
        var value = configuration?.GetValue<string>(configKey);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        Console.WriteLine($"[Elsa] {label}来源：配置键 {configKey}");
        return value.Trim();
    }

    /// <summary>
    /// 由每机签名密钥确定性派生口令：<c>Base64(SHA256(域标签 ‖ 签名密钥))</c>，44 个 ASCII 字符（256 bit 熵源）。
    ///
    /// <para>
    /// 之所以能这么用：签名密钥本身已经是"每机唯一 + 8 个服务共读同一文件并收敛"的引导密钥
    /// （见 <see cref="ElsaSigningKeyResolver"/> 的原子写 + 回读逻辑）。口令作为它的纯函数，
    /// 服务端与客户端必然算出同一个值；若签名密钥真的没收敛，JWT 校验会先一步响亮失败，不会退化成"登录悄悄 401"。
    /// </para>
    /// </summary>
    private static string DeriveFromSigningKey(IConfiguration? configuration)
    {
        var signingKey = ElsaSigningKeyResolver.Resolve(configuration);
        var material = Encoding.UTF8.GetBytes(DerivationLabel + signingKey);
        var hash = SHA256.HashData(material);
        return Convert.ToBase64String(hash);
    }
}
