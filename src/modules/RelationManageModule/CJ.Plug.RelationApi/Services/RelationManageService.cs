using CJ.Plug.Models.Relation;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Relation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class RelationManageService : BaseRepositoryService<CommonRelation, int>, IRelationManageService
{
    public RelationManageService(MainDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<CommonRelation>> GetAllRelationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<CommonRelation>()
            .ToListAsync();
    }

    public async Task<CommonRelation> CreateRealationAsync(CommonRelation request, CancellationToken cancellationToken = default)
    {
        // 开始事务
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                // 添加到Realationss表
                _dbContext.Set<CommonRelation>().Add(request);
                await _dbContext.SaveChangesAsync(cancellationToken);
                // 提交事务
                await transaction.CommitAsync(cancellationToken);

                return request;
            }
            catch (Exception)
            {
                // 发生异常时回滚事务
                await transaction.RollbackAsync();
                throw; // 或者根据需要处理异常
            }
        }
    }

    public async Task<bool> DeleteRealationAsync(CommonRelation request, CancellationToken cancellationToken = default)
    {
        try
        {
            var roleAId = request.RoleAId;
            var roleBId = request.RoleBId;
            Console.WriteLine("roleAId:" + roleAId);
            Console.WriteLine("roleBId:" + roleBId);
            if (roleAId == null && roleBId == null)
            {
                Console.WriteLine("request Id is null");
                return false;
            }
            //删除RoleA时
            if (roleAId != null && roleBId == null)
            {
                Console.WriteLine("start to delete by role A");
                var relations = _dbContext.Set<CommonRelation>().Where(r => r.RoleAId == roleAId)?.ToList();
                if (relations == null)
                {
                    return false;
                }
                foreach (var i in relations)
                {
                    _dbContext.Set<CommonRelation>().Remove(i);
                }
                await _dbContext.SaveChangesAsync();
                return true;
            }
            //删除RoleB时
            if (roleAId == null && roleBId != null)
            {
                Console.WriteLine("start to delete by role B");
                var re = _dbContext.Set<CommonRelation>().Where(r => r.RoleBId == roleBId)?.ToList();
                if (re == null)
                {
                    return false;
                }
                foreach (var i in re)
                {
                    _dbContext.Set<CommonRelation>().Remove(i);
                }
                await _dbContext.SaveChangesAsync();
                Console.WriteLine("delete role B relationship done");
                return true;
            }
            Console.WriteLine("start to delete by role A and B");
            //删除指定关联关系
            var item = _dbContext.Set<CommonRelation>().Where(r => r.RoleAId == roleAId && r.RoleBId == roleBId)?.FirstOrDefault();
            if (item == null)
            {
                return false;
            }
            _dbContext.Set<CommonRelation>().Remove(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw; // 或者根据需要处理异常
        }


    }

    public async Task<IEnumerable<CommonRelation?>> GetRelationsByCategoryAsync(string Category, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<CommonRelation>().Where(r => r.RelationCategory == Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<CommonRelation?>> GetRealationByFilterAsync(RelationFilter filter, CancellationToken cancellationToken = default)
    {
        // 这里可以根据 filter 的属性进行查询
        // 例如，如果 filter 中有 RoleAId 和 RoleBId，可以这样查询：
        var query = _dbContext.Set<CommonRelation>().AsQueryable();
        if (filter.RoleAId != null)
        {
            query = query.Where(r => r.RoleAId == filter.RoleAId);
        }
        if (filter.RoleBId != null)
        {
            query = query.Where(r => r.RoleBId == filter.RoleBId);
        }
        if (filter.RelationCategory != null)
        {
            query = query.Where(r => r.RelationCategory == filter.RelationCategory);
        }
        Console.WriteLine("get relation by filter:"+ query.Count());
        return await query.ToListAsync(cancellationToken);

    }

    public async Task<CommonRelation?> UpdateRealationAsync(CommonRelation request, CancellationToken cancellationToken = default)
    {    
        var relation=await _dbContext.Set<CommonRelation>().FindAsync(request.Id, cancellationToken);
        if (relation == null)
        {
            Console.WriteLine($"Relation {request.Id} not found");
            return null;
        }
        Console.WriteLine($"request:{JsonSerializer.Serialize(request)}");
        relation.RoleAId = request.RoleAId;
        relation.RoleBId = request.RoleBId;
        relation.RoleAName = request.RoleAName;
        relation.RoleBName = request.RoleBName;
        relation.RelationCategory = request.RelationCategory;
        relation.RelationType = request.RelationType;
        relation.RelationSetting = request.RelationSetting;
        await _dbContext.SaveChangesAsync();
        return relation;
    }
}

