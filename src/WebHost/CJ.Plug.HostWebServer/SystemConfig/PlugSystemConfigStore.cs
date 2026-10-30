using CJCore.Modules.Data;
using CJCore.Modules.SystemConfig.Abstractions;
using CJCore.Modules.ThreeRoleAdmin.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// CJCore 的公共契约 record 与 ThreeRoleAdmin 的持久化实体在本文件都要用；
// 二者语义不同（一个是接口 DTO，一个是 EF 实体），起别名避免读串。
using CoreConfigItem = CJCore.Modules.SystemConfig.Abstractions.SystemConfigItem;
using SysConfigRow = CJCore.Modules.ThreeRoleAdmin.Entities.SysConfig;

namespace CJ.Plug.HostWebServer.SystemConfig;

/// <summary>
/// CJPlug 的系统配置持久化实现（宿主侧，P7.1a）。
///
/// <para><b>为什么换了一整套底座</b>：<c>/system-config</c> 页面（P7.1）此前的写侧走本类、读侧仍走
/// CJPlug 私有模块的 HTTP 接口（<c>/api/systemconfig/*</c> → <c>SystemConfigItems</c> 表）。
/// P7.1a 把那个模块连同表一起退役 ⇒ 本类必须自己落到 CJCore 的持久化面上，
/// 否则 CJCore 默认的 <c>DefaultSystemConfigStore</c> 是<b>纯内存</b>，重启即丢配置。</para>
///
/// <para><b>为什么复用 <c>TRA_SysConfigs</c> 而不是新建一张 CJPlug 私有表</b>：
/// 该表（<c>CJCore.Modules.ThreeRoleAdmin</c> 的 <see cref="SysConfigRow"/>）是「键值 + 分类」的通用
/// 模块级配置表，随 P5 三员接管已存在于现场每一份 <c>main.db</c> 中 ⇒ <b>零迁移</b>。
/// 若新建表，P4 的自动迁移引擎面对“存量库已基线化”的现场并不能保证补建新表（需挪走 main.db 冷启动），
/// 升级成本与风险都远高于共用一张键值表。三条边界保证共用一个不串味：
/// <list type="number">
/// <item><b>写入</b>：本类所有行一律 <c>Category = "Plug"</c>；ThreeRoleAdmin 自己的行是
/// <c>JWT / Password / Security / UI</c>，两边不重叠。</item>
/// <item><b>读取</b>：<see cref="GetAllAsync"/> <b>只返回</b> <c>Category = "Plug"</c> 的行 ——
/// 不会把 <c>TRA_JwtKey</c> 一类的敏感值读进配置快照。</item>
/// <item><b>删除</b>：同样限定在本分类内，<b>不会</b>误删三员的配置行。</item>
/// </list>
/// 另一侧也核对过：ThreeRoleAdmin 只按具体 <c>Key</c>（<c>TRA_JwtKey</c> 等）读写本表、**不枚举全表**，
/// 因此多出若干 <c>Plug</c> 行不会改变它的行为。</para>
///
/// <para><b>生命周期</b>：<see cref="ISystemConfigStore"/> 由 <c>AddCJCoreSystemConfig</c> 以
/// <c>TryAddSingleton</c> 注册、被 <c>SystemConfigService</c>（Singleton）消费 ⇒ 本类必须是 Singleton，
/// 故注入 <see cref="IServiceScopeFactory"/> 并在每次操作内自建 scope 取 Scoped 的 <see cref="DataDbContext"/>。</para>
///
/// <para><b>⚠ 单写者语义</b>：本表全平台只有本进程写（页面保存 + 启动种子）。
/// 旧实现经 HTTP 打到“另一个进程”，存在“页面显示已保存、另一侧下次查询才可见”的缓存滞后；
/// 现在读写同进程同一份快照缓存 ⇒ <b>保存即生效</b>，该缺陷随退役一并消失。</para>
/// </summary>
public sealed class PlugSystemConfigStore(
    IServiceScopeFactory scopeFactory,
    ILogger<PlugSystemConfigStore> logger) : ISystemConfigStore
{
    /// <summary>
    /// 本宿主写入行的分类标记。对外可见（运维可直接按此列筛出 CJPlug 的配置行），
    /// 也是本类三条边界（写入标记 / 读取过滤 / 删除限定）共用的唯一字面量。
    /// </summary>
    public const string Category = "Plug";

    public async Task<IReadOnlyList<CoreConfigItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();

        var rows = await db.Set<SysConfigRow>()
            .AsNoTracking()
            .Where(r => r.Category == Category)
            .ToListAsync(cancellationToken);

        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .Select(r => new CoreConfigItem(r.Key, r.Value ?? string.Empty))
            .ToList();
    }

    /// <summary>整体覆盖语义的 upsert：按 <c>Key</c> 更新，不存在的插入。</summary>
    public async Task SaveAsync(IEnumerable<CoreConfigItem> items, CancellationToken cancellationToken = default)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i.Key)).ToList();
        if (list.Count == 0) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();

        var keys = list.Select(i => i.Key).ToList();
        var existing = await db.Set<SysConfigRow>()
            .Where(r => keys.Contains(r.Key))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var existingByKey = existing.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var item in list)
        {
            if (existingByKey.TryGetValue(item.Key, out var row))
            {
                // 分类是“本宿主”的标记，已有行若被别处改成其它分类，这里**不抢**（只更新值）。
                row.Value = item.Value;
                row.UpdatedAt = now;
            }
            else
            {
                db.Set<SysConfigRow>().Add(new SysConfigRow
                {
                    Key = item.Key,
                    Value = item.Value,
                    Category = Category,
                    UpdatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogDebug("[P7.1a] 系统配置已落库 {Count} 项（Category={Category}）", list.Count, Category);
    }

    /// <summary>
    /// 真删（幂等）：键不存在静默跳过。删除范围**限定在本分类**，不会碰到三员的配置行。
    /// </summary>
    public async Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0) return;

        var wanted = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (wanted.Count == 0) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();

        var rows = await db.Set<SysConfigRow>()
            .Where(r => r.Category == Category && wanted.Contains(r.Key))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return; // 幂等：没有可删的行就不写库

        db.Set<SysConfigRow>().RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken);
    }
}
