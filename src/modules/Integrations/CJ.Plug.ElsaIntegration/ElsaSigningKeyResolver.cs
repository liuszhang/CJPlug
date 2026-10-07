using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CJ.Plug.ElsaIntegration;

/// <summary>
/// Elsa 的 JWT 签名密钥解析器。
///
/// <para>
/// <b>为什么需要它</b>：CJPlug 原先在 <c>ElsaExtensions</c> 里硬编码 Elsa 的**公开默认值**
/// <c>sufficiently-large-secret-signing-key</c>。Elsa.Identity 3.9 的 TokenOptions 校验器规定
/// 「已知公开默认值只在 Development / Demo 环境允许」，因此**只要环境不是 Development**
/// （安装态、桌面端直接 <c>dotnet AppHost.dll</c>、任何非 VS 启动），ApiServer 与 ElsaApiServer
/// 都会以 <c>OptionsValidationException: SigningKey uses a known public default value</c>
/// → <c>Hosting failed to start</c> 退出。
/// </para>
///
/// <para>
/// <b>取值优先级</b>：① 环境变量 <c>CJPLUG_ELSA_SIGNING_KEY</c> → ② 配置键 <c>Elsa:SigningKey</c> →
/// ③ 已持久化的密钥文件（进程基目录下 <c>elsa-signing.key</c>）→ ④ 生成 64 字节随机密钥并落盘。
/// </para>
///
/// <para>
/// <b>为什么两个服务能共用同一把</b>：8 个服务共享同一输出目录，ApiServer 与 ElsaApiServer 的
/// <c>AppContext.BaseDirectory</c> 是同一个目录，故读写的是同一个 <c>elsa-signing.key</c>；
/// 首启并发由「临时文件 + Move 覆盖 + 回读」保证最终一致（谁先落盘以磁盘为准）。
/// </para>
///
/// <para>
/// 生成的是 64 字节随机数再 Base64（88 个 ASCII 字符），远高于 Elsa 的 256 bit 下限。
/// 落盘失败时退回内存密钥并**明确告警**（重启会换钥匙 → 已签发 token 失效），不静默降级到公开默认值。
/// </para>
/// </summary>
internal static class ElsaSigningKeyResolver
{
    /// <summary>环境变量名（运维/测试可用它注入固定密钥；CI 也可用）。</summary>
    internal const string EnvVarName = "CJPLUG_ELSA_SIGNING_KEY";

    /// <summary>配置键（appsettings 里可写 <c>"Elsa": { "SigningKey": "..." }</c>）。</summary>
    internal const string ConfigKey = "Elsa:SigningKey";

    /// <summary>密钥文件名（落在进程基目录 = 服务共享输出目录）。</summary>
    internal const string FileName = "elsa-signing.key";

    private static readonly object Gate = new();
    private static string? _cached;

    /// <summary>解析（并缓存）签名密钥；<paramref name="configuration"/> 可为 null（配置档位自动跳过）。</summary>
    internal static string Resolve(IConfiguration? configuration = null)
    {
        if (_cached is { Length: > 0 })
            return _cached!;

        lock (Gate)
        {
            if (_cached is { Length: > 0 })
                return _cached!;

            var key = TryEnvVar() ?? TryConfiguration(configuration) ?? TryPersistedFile() ?? GenerateAndPersist();
            _cached = key;
            return key;
        }
    }

    private static string? TryEnvVar()
    {
        var value = Environment.GetEnvironmentVariable(EnvVarName);
        if (string.IsNullOrWhiteSpace(value))
            return null;
        Console.WriteLine($"[Elsa] 签名密钥来源：环境变量 {EnvVarName}");
        return value.Trim();
    }

    private static string? TryConfiguration(IConfiguration? configuration)
    {
        var value = configuration?.GetValue<string>(ConfigKey);
        if (string.IsNullOrWhiteSpace(value))
            return null;
        Console.WriteLine($"[Elsa] 签名密钥来源：配置键 {ConfigKey}");
        return value.Trim();
    }

    private static string? TryPersistedFile()
    {
        var path = KeyFilePath();
        try
        {
            if (!File.Exists(path))
                return null;
            var value = File.ReadAllText(path, Encoding.UTF8).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return null;
            Console.WriteLine($"[Elsa] 签名密钥来源：已持久化文件 {path}");
            return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Elsa] ⚠ 读取签名密钥文件失败（转下一来源）：{path}：{ex.Message}");
            return null;
        }
    }

    private static string GenerateAndPersist()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var path = KeyFilePath();
        try
        {
            // 原子写：先写临时文件再 Move 覆盖 —— 避免并发首启写出半截文件；
            // 抢不到（另一个服务已写）则以磁盘上的为准，保证两者最终用同一把密钥。
            var tmp = $"{path}.{Environment.ProcessId}.tmp";
            File.WriteAllText(tmp, key, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(tmp, path, overwrite: true);

            var onDisk = File.ReadAllText(path, Encoding.UTF8).Trim();
            if (!string.IsNullOrWhiteSpace(onDisk) && onDisk != key)
            {
                Console.WriteLine($"[Elsa] 检测到他进程已生成签名密钥，采用磁盘上的：{path}");
                return onDisk;
            }

            Console.WriteLine($"[Elsa] 已生成并持久化 JWT 签名密钥（64 字节随机 / Base64）：{path}");
            return key;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Elsa] ⚠ 签名密钥落盘失败：{path}：{ex.Message}");
            Console.WriteLine("[Elsa]    本次使用内存临时密钥；重启会更换密钥，已签发的 token 将失效（请修复目录写权限）。");
            return key;
        }
    }

    private static string KeyFilePath() => Path.Combine(AppContext.BaseDirectory, FileName);
}
