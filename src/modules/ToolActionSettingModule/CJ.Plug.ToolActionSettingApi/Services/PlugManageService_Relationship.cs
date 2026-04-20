using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.EntityFrameworkCore;

public partial class PlugManageService : IPlugManageService
{

    public async Task<IEnumerable<PlugToPlugAction>> GetAllRealationssAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PlugToPlugAction>()
            .ToListAsync();
    }

    public async Task<PlugToPlugAction> CreateRealationAsync(PlugToPlugAction request, CancellationToken cancellationToken = default)
    {
        // 开始事务
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var toolItem = request;

                // 添加到Realationss表
                _dbContext.Set<PlugToPlugAction>().Add(toolItem);
                await _dbContext.SaveChangesAsync(cancellationToken);
                // 提交事务
                await transaction.CommitAsync(cancellationToken);

                return toolItem;
            }
            catch (Exception)
            {
                // 发生异常时回滚事务
                await transaction.RollbackAsync();
                throw; // 或者根据需要处理异常
            }
        }
    }

    public async Task<bool> DeleteRealationsAsync(PlugToPlugAction request, CancellationToken cancellationToken = default)
    {
        try
        {
            var plugId = request.PlugId;
            var plugActionDefinitionId = request.PlugActionDefinitionId;
            Console.WriteLine("plugId:" + plugId);
            Console.WriteLine("plugActionDefinitionId:" + plugActionDefinitionId);
            if (plugId == null && plugActionDefinitionId == null)
            {
                Console.WriteLine("plugId or plugActionDefinitionId is null");
                return false;
            }
            //删除插头时
            if (plugId != null && plugActionDefinitionId == null)
            {
                Console.WriteLine("start to delete by plug");
                var relations = _dbContext.Set<PlugToPlugAction>().Where(p => p.PlugId == plugId)?.ToList();
                if (relations == null)
                {
                    return false;
                }
                foreach (var i in relations)
                {
                    _dbContext.Set<PlugToPlugAction>().Remove(i);
                }
                await _dbContext.SaveChangesAsync();
                return true;
            }
            //删除插头动作时
            if (plugId == null && plugActionDefinitionId != null)
            {
                Console.WriteLine("start to delete by plugaction");
                var re = _dbContext.Set<PlugToPlugAction>().Where(p => p.PlugActionDefinitionId == plugActionDefinitionId)?.ToList();
                if (re == null)
                {
                    return false;
                }
                foreach (var i in re)
                {
                    _dbContext.Set<PlugToPlugAction>().Remove(i);
                }
                await _dbContext.SaveChangesAsync();
                Console.WriteLine("delete plugaction relationship done");
                return true;
            }
            Console.WriteLine("start to delete by plugaction and plug");
            //删除指定关联关系
            var item = _dbContext.Set<PlugToPlugAction>().Where(p => p.PlugId == plugId && p.PlugActionDefinitionId == plugActionDefinitionId)?.FirstOrDefault();
            if (item == null)
            {
                return false;
            }
            _dbContext.Set<PlugToPlugAction>().Remove(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw; // 或者根据需要处理异常
        }


    }
    public async Task<bool> DeleteRealationsAsync(int? plugId,string? plugActionDefinitionId)
    {
        try
        {
            if (plugId == null || plugActionDefinitionId == null)
            {
                return false;
            }
            if (plugId != null && plugActionDefinitionId == null)
            {
                var relations = _dbContext.Set<PlugToPlugAction>().Where(p => p.PlugId == plugId)?.ToList();
                if (relations == null)
                {
                    return false;
                }
                foreach (var i in relations)
                {
                    _dbContext.Set<PlugToPlugAction>().Remove(i);
                }
                await _dbContext.SaveChangesAsync();
                return true;
            }
            if (plugId == null && plugActionDefinitionId != null)
            {
                var plugAction = await _dbContext.Set<PlugAction>().Where(a => a.DefinitionId == plugActionDefinitionId).FirstOrDefaultAsync();
                if (plugAction == null || plugAction.IsRootPlug)
                {
                    return false;
                }
                var re = _dbContext.Set<PlugToPlugAction>().Where(p => p.PlugActionDefinitionId == plugActionDefinitionId)?.ToList();
                if (re == null)
                {
                    return false;
                }
                foreach (var i in re)
                {
                    _dbContext.Set<PlugToPlugAction>().Remove(i);
                }
                await _dbContext.SaveChangesAsync();
                return true;
            }
            var item = _dbContext.Set<PlugToPlugAction>().Where(p => p.PlugId == plugId && p.PlugActionDefinitionId == plugActionDefinitionId)?.FirstOrDefault();
            if (item == null)
            {
                return false;
            }
            _dbContext.Set<PlugToPlugAction>().Remove(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw; // 或者根据需要处理异常
        }
        
        
    }

}

