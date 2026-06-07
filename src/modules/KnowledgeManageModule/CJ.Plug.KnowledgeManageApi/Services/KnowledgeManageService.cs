using CJ.Plug.Models.Knowledge;
using CJ.Plug.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.KnowledgeManageApi;

/// <summary>
/// 知识管理服务实现：知识库 CRUD + 文件夹 CRUD + 知识条目 CRUD + 搜索 + 级联删除
/// </summary>
public class KnowledgeManageService : IKnowledgeManageService
{
    private readonly MainDbContext _dbContext;

    public KnowledgeManageService(MainDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ===== 知识库 CRUD =====

    public async Task<List<KnowledgeBase>> GetAllBasesAsync(CancellationToken ct = default)
    {
        return await _dbContext.Set<KnowledgeBase>()
            .OrderBy(b => b.Order)
            .ThenBy(b => b.Id)
            .ToListAsync(ct);
    }

    public async Task<KnowledgeBase?> GetBaseByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Set<KnowledgeBase>().FindAsync([id], ct);
    }

    public async Task<KnowledgeBase> CreateBaseAsync(KnowledgeBase kb, CancellationToken ct = default)
    {
        kb.CreatedAt = DateTime.UtcNow;
        kb.UpdatedAt = DateTime.UtcNow;
        _dbContext.Set<KnowledgeBase>().Add(kb);
        await _dbContext.SaveChangesAsync(ct);
        return kb;
    }

    public async Task<KnowledgeBase?> UpdateBaseAsync(KnowledgeBase kb, CancellationToken ct = default)
    {
        var existing = await _dbContext.Set<KnowledgeBase>().FindAsync([kb.Id], ct);
        if (existing is null) return null;

        existing.Name = kb.Name;
        existing.Description = kb.Description;
        existing.IconUrl = kb.IconUrl;
        existing.Order = kb.Order;
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteBaseAsync(int id, CancellationToken ct = default)
    {
        var kb = await _dbContext.Set<KnowledgeBase>().FindAsync([id], ct);
        if (kb is null) return false;

        _dbContext.Set<KnowledgeBase>().Remove(kb);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    // ===== 文件夹 CRUD =====

    /// <summary>
    /// 根据知识库ID获取其下所有文件夹（扁平列表，前端构建树形结构）
    /// </summary>
    public async Task<List<KnowledgeFolder>> GetFoldersByBaseAsync(int baseId, CancellationToken ct = default)
    {
        return await _dbContext.Set<KnowledgeFolder>()
            .Where(f => f.BaseId == baseId)
            .OrderBy(f => f.Order)
            .ThenBy(f => f.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 根据ID获取文件夹
    /// </summary>
    public async Task<KnowledgeFolder?> GetFolderByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Set<KnowledgeFolder>().FindAsync([id], ct);
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    public async Task<KnowledgeFolder> CreateFolderAsync(KnowledgeFolder folder, CancellationToken ct = default)
    {
        folder.CreatedAt = DateTime.UtcNow;
        folder.UpdatedAt = DateTime.UtcNow;
        _dbContext.Set<KnowledgeFolder>().Add(folder);
        await _dbContext.SaveChangesAsync(ct);
        return folder;
    }

    /// <summary>
    /// 更新文件夹
    /// </summary>
    public async Task<KnowledgeFolder?> UpdateFolderAsync(KnowledgeFolder folder, CancellationToken ct = default)
    {
        var existing = await _dbContext.Set<KnowledgeFolder>().FindAsync([folder.Id], ct);
        if (existing is null) return null;

        existing.BaseId = folder.BaseId;
        existing.Name = folder.Name;
        existing.ParentId = folder.ParentId;
        existing.Description = folder.Description;
        existing.Icon = folder.Icon;
        existing.Order = folder.Order;
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return existing;
    }

    /// <summary>
    /// 删除文件夹（递归级联删除子文件夹及其知识条目）
    /// </summary>
    public async Task<bool> DeleteFolderAsync(int id, CancellationToken ct = default)
    {
        var folder = await _dbContext.Set<KnowledgeFolder>().FindAsync([id], ct);
        if (folder is null) return false;

        // 递归删除所有子文件夹及其条目
        await DeleteFolderRecursiveAsync(id, ct);

        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// 递归删除文件夹及其子内容
    /// </summary>
    private async Task DeleteFolderRecursiveAsync(int folderId, CancellationToken ct)
    {
        // 查找所有子文件夹
        var childFolders = await _dbContext.Set<KnowledgeFolder>()
            .Where(f => f.ParentId == folderId)
            .ToListAsync(ct);

        // 递归删除每个子文件夹
        foreach (var child in childFolders)
        {
            await DeleteFolderRecursiveAsync(child.Id, ct);
        }

        // 删除当前文件夹下的所有知识条目
        var items = await _dbContext.Set<KnowledgeItem>()
            .Where(i => i.FolderId == folderId)
            .ToListAsync(ct);
        _dbContext.Set<KnowledgeItem>().RemoveRange(items);

        // 删除当前文件夹
        var folder = await _dbContext.Set<KnowledgeFolder>().FindAsync([folderId], ct);
        if (folder is not null)
        {
            _dbContext.Set<KnowledgeFolder>().Remove(folder);
        }
    }

    // ===== 知识条目 CRUD =====

    /// <summary>
    /// 根据文件夹ID获取知识条目列表；folderId 为 null 时返回所有条目
    /// </summary>
    public async Task<List<KnowledgeItem>> GetItemsByFolderAsync(int? folderId, CancellationToken ct = default)
    {
        var query = _dbContext.Set<KnowledgeItem>().AsQueryable();

        if (folderId.HasValue)
        {
            query = query.Where(i => i.FolderId == folderId.Value);
        }

        return await query
            .OrderBy(i => i.Order)
            .ThenByDescending(i => i.UpdatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 根据知识库ID获取所有条目（通过文件夹关联）
    /// </summary>
    public async Task<List<KnowledgeItem>> GetItemsByBaseAsync(int baseId, CancellationToken ct = default)
    {
        var folderIds = await _dbContext.Set<KnowledgeFolder>()
            .Where(f => f.BaseId == baseId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        if (folderIds.Count == 0)
            return new List<KnowledgeItem>();

        return await _dbContext.Set<KnowledgeItem>()
            .Where(i => folderIds.Contains(i.FolderId))
            .OrderBy(i => i.Order)
            .ThenByDescending(i => i.UpdatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 根据ID获取知识条目
    /// </summary>
    public async Task<KnowledgeItem?> GetItemByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Set<KnowledgeItem>().FindAsync([id], ct);
    }

    /// <summary>
    /// 创建知识条目
    /// </summary>
    public async Task<KnowledgeItem> CreateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _dbContext.Set<KnowledgeItem>().Add(item);
        await _dbContext.SaveChangesAsync(ct);
        return item;
    }

    /// <summary>
    /// 更新知识条目
    /// </summary>
    public async Task<KnowledgeItem?> UpdateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        var existing = await _dbContext.Set<KnowledgeItem>().FindAsync([item.Id], ct);
        if (existing is null) return null;

        existing.FolderId = item.FolderId;
        existing.Title = item.Title;
        existing.Content = item.Content;
        existing.Tags = item.Tags;
        existing.Order = item.Order;
        existing.IsEnabled = item.IsEnabled;
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return existing;
    }

    /// <summary>
    /// 删除知识条目
    /// </summary>
    public async Task<bool> DeleteItemAsync(int id, CancellationToken ct = default)
    {
        var item = await _dbContext.Set<KnowledgeItem>().FindAsync([id], ct);
        if (item is null) return false;

        _dbContext.Set<KnowledgeItem>().Remove(item);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    // ===== 搜索 =====

    /// <summary>
    /// 搜索知识条目，支持关键字和标签组合过滤（AND 关系），可按知识库限定范围
    /// </summary>
    public async Task<List<KnowledgeItem>> SearchItemsAsync(string? keyword, string? tag, int? baseId = null, CancellationToken ct = default)
    {
        var query = _dbContext.Set<KnowledgeItem>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(i =>
                EF.Functions.Like(i.Title, pattern) ||
                EF.Functions.Like(i.Content, pattern));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagPattern = $"%{tag.Trim()}%";
            query = query.Where(i => EF.Functions.Like(i.Tags, tagPattern));
        }

        if (baseId.HasValue)
        {
            query = query.Where(i => i.Folder != null && i.Folder.BaseId == baseId.Value);
        }

        return await query
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(ct);
    }

    // ===== 辅助 =====

    /// <summary>
    /// 检查文件夹是否有子内容（子文件夹或知识条目）
    /// </summary>
    public async Task<bool> HasChildContentAsync(int folderId, CancellationToken ct = default)
    {
        var hasChildren = await _dbContext.Set<KnowledgeFolder>()
            .AnyAsync(f => f.ParentId == folderId, ct);
        if (hasChildren) return true;

        var hasItems = await _dbContext.Set<KnowledgeItem>()
            .AnyAsync(i => i.FolderId == folderId, ct);
        return hasItems;
    }
}
