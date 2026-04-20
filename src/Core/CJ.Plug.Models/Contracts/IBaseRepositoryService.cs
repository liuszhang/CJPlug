namespace CJ.Plug.Models.Contracts
{
    /// <summary>
    /// 基础服务接口，定义通用 CRUD 操作
    /// </summary>
    /// <typeparam name="TEntity">实体类型（如 PlugStatusData、ActionData 等）</typeparam>
    /// <typeparam name="TId">实体主键类型（如 int、string）</typeparam>
    public interface IBaseRepositoryService<TEntity, TId>
        where TEntity : class
    {
        // 新增
        Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
        // 根据 ID 查询
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        // 根据 Definition ID 查询
        Task<TEntity?> GetByPlugDefinitionIdAsync(string PlugDefinitionId, CancellationToken cancellationToken = default);
        Task<TEntity?> GetByDefinitionIdAsync(string DefinitionId, CancellationToken cancellationToken = default);
        // 查询所有
        Task<List<TEntity>?> GetAllAsync(CancellationToken cancellationToken = default);
        // 更新（含批量子实体处理）
        Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        // 删除
        Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);
        // 删除(根据ByDefinitionId)
        Task<bool> DeleteByPlugDefinitionIdAsync(string DefinitionId, CancellationToken cancellationToken = default);

    }

    // 辅助接口：标记实体有主键（统一获取主键的方式）
    public interface IHasId<TId>
    {
        TId Id { get; set; }
    }
}
