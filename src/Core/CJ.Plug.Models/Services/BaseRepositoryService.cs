using CJ.Plug.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.Models.Services
{
    public class BaseRepositoryService<TEntity, TId> : IBaseRepositoryService<TEntity, TId>
    where TEntity : class
    {
        protected readonly MainDbContext _dbContext;

        // 通过构造函数注入 DbContext
        public BaseRepositoryService(MainDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbContext.Database.EnsureCreated();
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
}
