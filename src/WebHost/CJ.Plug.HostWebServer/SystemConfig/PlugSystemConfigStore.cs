using CJ.Plug.SystemConfigApiClient;
using CJ.Plug.SystemConfigModel.Models;
using CJCore.Modules.SystemConfig.Abstractions;
using Microsoft.Extensions.Logging;

// CJCore 的公共契约 record 与 CJPlug 自己的实体同名（都是 SystemConfigItem），
// 两边在本文件都要用到 ⇒ 各起一个别名，避免 CS0104 歧义。
using CoreConfigItem = CJCore.Modules.SystemConfig.Abstractions.SystemConfigItem;
using PlugConfigRow = CJ.Plug.SystemConfigModel.Models.SystemConfigItem;

namespace CJ.Plug.HostWebServer.SystemConfig;

/// <summary>
/// CJPlug 的系统配置持久化实现（宿主侧）：落到 CJPlug 现有的 <c>SystemConfigItems</c> 表。
///
/// <para><b>为什么不新建表</b>（偏离原方案 7.1a 的“连同其表一起退役”）：本表的消费方不在本进程 ——
/// <c>CJ.Plug.PlugBaseCore/Services/ToolExecuteService.cs</c> 经 <c>MainApiClient.SystemConfigApiClient</c>
/// 打 <b>ApiServer</b> 进程的 <c>/api/systemconfig/*</c> 读 <c>RemoteViewMode</c>，
/// 而真正读到并执行的是跑在工位机上的 Station 端（那边根本没有数据库）。
/// 换掉表/更深一层换掉读法，等于要求 ApiServer 也整套接入 CJCore 系统配置模块（属 P5 之后的活），
/// 会让 P7 从“一个页面改造”膨胀成“跨三进程的数据面迁移”。
/// ⇒ <b>P7 收口成：写侧换到 /system-config，读侧照旧，两者共用同一张表</b>，
/// 新旧写入走的都是同一个 <c>Key</c>（见 <see cref="PlugSystemConfigKeys"/>），数据天然一致。</para>
///
/// <para><b>⚠ 跨进程缓存</b>：ApiServer 侧若有进程内缓存，本页保存后它不会立即失效。
/// 现状同样存在（旧页面写库后 ApiServer 也是下次查询才可见），本节不引入新缺陷。</para>
/// </summary>
public sealed class PlugSystemConfigStore : ISystemConfigStore
{
    private readonly ISystemConfigApiClient _api;
    private readonly ILogger<PlugSystemConfigStore> _logger;

    public PlugSystemConfigStore(
        CJ.Plug.SystemConfigApiClient.ISystemConfigApiClient api,
        ILogger<PlugSystemConfigStore> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CoreConfigItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _api.GetAllAsync(cancellationToken);

        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .Select(r => new CoreConfigItem(r.Key, r.Value))
            .ToList();
    }

    /// <summary>整体覆盖语义的 upsert：按 <c>Key</c> 更新，不存在的插入（ApiServer 端保证幂等）。</summary>
    public async Task SaveAsync(IEnumerable<CoreConfigItem> items, CancellationToken cancellationToken = default)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i.Key)).ToList();
        if (list.Count == 0) return;

        foreach (var item in list)
        {
            // ⚠ CJCore 的公共契约 <c>SystemConfigItem</c> 只有 (Key, Value)，没有 Description
            //   ⇒ 描述一律传 null。旧 CJPlug 表里那列会保持原值或被 ApiServer 置空，
            //     属可接受损失（该描述列不参与任何读取逻辑）。
            var row = await _api.SetValueAsync(item.Key, item.Value, null, cancellationToken);
            if (row is null)
            {
                // ApiServer 返回空 ⇒ 该键既没更新也没新增。不静默吞掉，否则页面显示“保存成功”而库里没变。
                _logger.LogWarning("[P7.1] 系统配置项 {Key} 保存未生效（ApiServer 返回空）", item.Key);
            }
        }
    }

    /// <summary>
    /// 真删（幂等）。<c>ISystemConfigStore</c> 的第三条 Fork 是给“动态键集”页签用的；
    /// CJPlug 目前只有固定开关，但接口要求实现。
    /// ⚠ <b>现有 HTTP API 面没有 delete</b>（<c>MapSystemConfigApi</c> 只暴露 getAll/get/set）
    /// ⇒ 这里只能记一条警告，不可假报成功。要支持删除须先在 ApiServer 加 Delete 端点。
    /// </summary>
    public Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0) return Task.CompletedTask;

        _logger.LogWarning(
            "[P7.1] 收到删除请求 {Count} 个键（{Keys}），但 ApiServer 尚未提供 delete 端点，本次未执行删除。",
            keys.Count, string.Join(", ", keys));

        return Task.CompletedTask;
    }

}
