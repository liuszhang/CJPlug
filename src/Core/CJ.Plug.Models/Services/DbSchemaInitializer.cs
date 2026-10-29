using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace CJ.Plug.Models.Services;

/// <summary>
/// 数据库结构就绪保障（2026-10-29《CJPlug 接入 CJCore》P3）。
/// <para>
/// 现状：8 个业务 Service 在<b>构造函数</b>里各自调用 <c>Database.EnsureCreated()</c>，
/// 首进程冷启动一瞬间会对同一个库连打 8 次（每次都要枚举库中全部表做 HasTables 判定），
/// 而这些 Service 多为 Scoped ⇒ 后续每个请求再来一轮。
/// </para>
/// <para>
/// 本类把它收敛成「<b>同一进程内、同一（上下文类型 + 连接串）只执行一次</b>」：
/// 冷启动从 8 次降为 1 次，稳态请求降为 0 次，同时收窄多进程并发建表的竞态窗口。
/// </para>
/// <para>
/// ⚠ 为什么这里<b>没有</b>换成 <c>Database.Migrate()</c>：
/// <c>MainDbContext</c> 的模型是<b>运行时动态拼装</b>的（<c>OnModelCreating</c> 里遍历
/// DI 收集到的 <c>IModuleDbConfig</c>），EF 设计时拿不到完整模型 ⇒ 生成出来的迁移快照
/// 必然缺表，此时改走 Migrate 只会让缺表更隐蔽（"迁移都成功了为什么不建表"）。
/// 真正的迁移基线留到 P4：业务表交给 CJCore Data 引擎（<c>EnableAutoMigration</c> 记账），
/// <c>MainDbContext</c> 只剩 Identity 固定模型后再生成快照。
/// </para>
/// </summary>
public static class DbSchemaInitializer
{
    private static readonly ConcurrentDictionary<string, bool> _done = new();
    private static readonly ConcurrentDictionary<string, object> _gates = new();

    /// <summary>进程内幂等地确保库结构已创建（等价于一次 <see cref="DatabaseFacade.EnsureCreated"/>）。</summary>
    public static void EnsureCreatedOnce(DbContext db)
    {
        // 键 = 上下文类型 + 连接串：同库不同上下文（P4 期间 DataDbContext 与 MainDbContext 同库）各自计一次
        var key = $"{db.GetType().FullName}|{db.Database.GetConnectionString()}";

        if (_done.ContainsKey(key)) return;

        var gate = _gates.GetOrAdd(key, _ => new object());
        lock (gate)
        {
            if (_done.ContainsKey(key)) return;

            db.Database.EnsureCreated();
            _done[key] = true;
        }
    }
}
