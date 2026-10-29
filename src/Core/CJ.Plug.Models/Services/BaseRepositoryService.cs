using CJ.Plug.Models.Contracts;
using CJCore.Modules.Data;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.Models.Services
{
    /// <summary>
    /// 通用仓储基类（<b>上下文类型由调用方指定</b>）。
    /// </summary>
    /// <remarks>
    /// <para>P4（2026-10-29）改造：原实现把上下文类型写死为 <c>MainDbContext</c>。
    /// 业务表迁入 CJCore 的 <c>DataDbContext</c> 后，仍留在身份侧的仓储（如 <c>UserManageService</c>，
    /// 实体是 <c>User</c>，表在 <c>MainDbContext</c> 里）不能跟着走 —— 否则 <c>Set&lt;User&gt;()</c>
    /// 会因模型里没有该实体而抛 <c>InvalidOperationException</c>。
    /// 因此泛型化上下文类型；<b>绝大多数调用方无需改动</b>（见下方两泛型兼容形态）。</para>
    /// <para>⚠ 已移除构造函数里的 <c>EnsureCreatedOnce</c>：业务库建表由 CJCore 自动迁移引擎
    /// （<c>AutoMigrationHostedService</c>）在 Kestrel 监听前完成，仓储构造期再建一次会形成第二条建表通道。</para>
    /// </remarks>
    public class BaseRepositoryService<TEntity, TId, TContext> : IBaseRepositoryService<TEntity, TId>
    where TEntity : class
    where TContext : DbContext
    {
        protected readonly TContext _dbContext;

        // 通过构造函数注入 DbContext
        public BaseRepositoryService(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 通用查询（根据ID）
        public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);
        }

        // 通用查询（根据Definition ID）
        public virtual async Task<TEntity?> GetByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            // 使用Where条件查询非主键字段
            return await _dbContext.Set<TEntity>()
                .FirstOrDefaultAsync(
                    entity => EF.Property<string>(entity, "DefinitionId") == definitionId,
                    cancellationToken
                );
        }

        // 通用查询（根据PlugDefinition ID）
        public virtual async Task<TEntity?> GetByPlugDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            // 使用Where条件查询非主键字段
            return await _dbContext.Set<TEntity>()
                .FirstOrDefaultAsync(
                    entity => EF.Property<string>(entity, "PlugDefinitionId") == definitionId,
                    cancellationToken
                );
        }

        // 通用查询（所有）
        public virtual async Task<List<TEntity>?> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TEntity>().ToListAsync(cancellationToken);
        }

        // 通用新增
        public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbContext.Set<TEntity>().Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        // 通用更新（简化版，实际可根据需要扩展）
        public virtual async Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbContext.Set<TEntity>().Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        // 通用删除
        public virtual async Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity == null) return false;
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        // 通用删除（根据PlugDefinitionId）
        public virtual async Task<bool> DeleteByPlugDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Set<TEntity>()
                .FirstOrDefaultAsync(
                    entity => EF.Property<string>(entity, "PlugDefinitionId") == definitionId,
                    cancellationToken
                );
            if (entity == null) return false;
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        // 其他方法省略...
    }

    /// <summary>
    /// 通用仓储基类（<b>两泛型兼容形态</b>）：上下文固定为业务库 <see cref="DataDbContext"/>。
    /// </summary>
    /// <remarks>
    /// 保留此签名是为了让既有 <c>: BaseRepositoryService&lt;TEntity, TId&gt;</c> 的 9 个仓储子类
    /// 一行不改。只有身份侧仓储（实体表在 <c>MainDbContext</c>）需要显式写三泛型形态。
    /// </remarks>
    public class BaseRepositoryService<TEntity, TId> : BaseRepositoryService<TEntity, TId, DataDbContext>
        where TEntity : class
    {
        public BaseRepositoryService(DataDbContext dbContext) : base(dbContext)
        {
        }
    }
}
