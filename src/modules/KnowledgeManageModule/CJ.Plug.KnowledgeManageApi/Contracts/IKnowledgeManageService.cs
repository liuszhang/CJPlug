using CJ.Plug.Models.Knowledge;

namespace CJ.Plug.KnowledgeManageApi;

/// <summary>
/// 知识管理服务接口 - 不继承 IBaseRepositoryService，独立接口
/// </summary>
public interface IKnowledgeManageService
{
    // 知识库 CRUD
    Task<List<KnowledgeBase>> GetAllBasesAsync(CancellationToken ct = default);
    Task<KnowledgeBase?> GetBaseByIdAsync(int id, CancellationToken ct = default);
    Task<KnowledgeBase> CreateBaseAsync(KnowledgeBase kb, CancellationToken ct = default);
    Task<KnowledgeBase?> UpdateBaseAsync(KnowledgeBase kb, CancellationToken ct = default);
    Task<bool> DeleteBaseAsync(int id, CancellationToken ct = default);

    // 文件夹 CRUD
    Task<List<KnowledgeFolder>> GetFoldersByBaseAsync(int baseId, CancellationToken ct = default);
    Task<KnowledgeFolder?> GetFolderByIdAsync(int id, CancellationToken ct = default);
    Task<KnowledgeFolder> CreateFolderAsync(KnowledgeFolder folder, CancellationToken ct = default);
    Task<KnowledgeFolder?> UpdateFolderAsync(KnowledgeFolder folder, CancellationToken ct = default);
    Task<bool> DeleteFolderAsync(int id, CancellationToken ct = default);

    // 知识条目 CRUD
    Task<List<KnowledgeItem>> GetItemsByFolderAsync(int? folderId, CancellationToken ct = default);
    Task<List<KnowledgeItem>> GetItemsByBaseAsync(int baseId, CancellationToken ct = default);
    Task<KnowledgeItem?> GetItemByIdAsync(int id, CancellationToken ct = default);
    Task<KnowledgeItem> CreateItemAsync(KnowledgeItem item, CancellationToken ct = default);
    Task<KnowledgeItem?> UpdateItemAsync(KnowledgeItem item, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(int id, CancellationToken ct = default);

    // 搜索
    Task<List<KnowledgeItem>> SearchItemsAsync(string? keyword, string? tag, int? baseId = null, CancellationToken ct = default);

    // 辅助：检查文件夹是否有子内容
    Task<bool> HasChildContentAsync(int folderId, CancellationToken ct = default);
}
