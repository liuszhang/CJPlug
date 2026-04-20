using CJ.Plug.Models.LogModels;


using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class PDZManageService : IPDZManageService
{
    private readonly MainDbContext _dbContext;
    public PDZManageService(MainDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task<PlugDataZone?> CreatePDZ(PlugDataZone PDZ, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(PDZ.PDZWorkPath))
        {
            if (string.IsNullOrEmpty(PDZ.JobDefinitionId))
            {
                PDZ.PDZWorkPath = Path.Combine(
                    PDZ.UserName ?? "null",
                    //FileFolderType.Design.ToString(),
                    PDZ.PDZId);
            }
            else
            {
                PDZ.PDZWorkPath = Path.Combine(
                    PDZ.UserName ?? "null",
                    //FileFolderType.Job.ToString(),
                    PDZ.PDZId);
            }
        }

        
        _dbContext.Set<PlugDataZone>().Add(PDZ);
        await _dbContext.SaveChangesAsync();
        return PDZ;
    }

    public async Task<PlugDataZone?> UpdatePDZ(PlugDataZone PDZ, CancellationToken cancellationToken = default)
    {
        // 1. 查询现有实体及其所有导航属性（必须Include所有需要处理的列表）
        var existingPDZ = await _dbContext.Set<PlugDataZone>()
            .Include(p => p.PDZVariables)
            .Include(p => p.PlugDatas)
            .Include(p => p.PlugVariableDatas)
            .Include(p => p.PlugStatusDatas)
            .Include(p => p.ActionDatas)
            .Include(p => p.ActionVariableDatas)
            .Include(p => p.FlowchartDatas)
            .Include(p => p.DataFlowDatas)
            .FirstOrDefaultAsync(p => p.PDZId == PDZ.PDZId, cancellationToken);

        if (existingPDZ == null)
        {
            await CreatePDZ(PDZ, cancellationToken);
            return PDZ;
        }
        // 更新现有实体的基本属性
        _dbContext.Entry(existingPDZ).CurrentValues.SetValues(PDZ);

        // 2. 处理所有列表（调用通用方法，每个列表单独处理）
        // 2.1 处理原有的 PDZVariables
        UpdateChildList(
            existingList: existingPDZ.PDZVariables,
            incomingList: PDZ.PDZVariables ?? new List<PDZVariable>(), // 避免传入null
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 2.2 处理 PlugDatas（假设其主键是 Id）
        UpdateChildList(
            existingList: existingPDZ.PlugDatas ?? new List<PlugData>(),
            incomingList: PDZ.PlugDatas ?? new List<PlugData>(),
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 2.3 处理 PlugVariableDatas（假设主键是 Id）
        UpdateChildList(
            existingList: existingPDZ.PlugVariableDatas ?? new List<PlugVariableData>(),
            incomingList: PDZ.PlugVariableDatas ?? new List<PlugVariableData>(),
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 2.4 处理 PlugStatusDatas（假设主键是 Id）
        UpdateChildList(
            existingList: existingPDZ.PlugStatusDatas ?? new List<PlugStatusData>(),
            incomingList: PDZ.PlugStatusDatas ?? new List<PlugStatusData>(),
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 2.5 处理 ActionDatas（假设主键是 Id）
        UpdateChildList(
            existingList: existingPDZ.ActionDatas ?? new List<ActionData>(),
            incomingList: PDZ.ActionDatas ?? new List<ActionData>(),
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 2.6 处理 ActionVariableDatas（假设主键是 Id）
        UpdateChildList(
            existingList: existingPDZ.ActionVariableDatas ?? new List<ActionVariableData>(),
            incomingList: PDZ.ActionVariableDatas ?? new List<ActionVariableData>(),
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 2.7 处理 FlowchartDatas（假设主键是 Id）
        UpdateChildList(
            existingList: existingPDZ.FlowchartDatas ?? new List<FlowchartData>(),
            incomingList: PDZ.FlowchartDatas ?? new List<FlowchartData>(),
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 2.8 处理 DataFlowDatas（假设主键是 Id）
        UpdateChildList(
            existingList: existingPDZ.DataFlowDatas ?? new List<DataFlowData>(),
            incomingList: PDZ.DataFlowDatas ?? new List<DataFlowData>(),
            getId: item => item.Id, // 指定PDZVariable的主键是Id
            isNewItem: item => item.Id == null || item.Id == 0 // 新增项判断逻辑
        );

        // 3. 保存所有更改（所有列表的操作在同一事务中提交）
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existingPDZ;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            CLog.Error("并发更新冲突: {Message}", ex.Message);
            throw;
        }
        catch (DbUpdateException ex)
        {
            CLog.Error("数据库更新错误: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            CLog.Error("更新 PDZ 时发生意外错误: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 通用处理列表的更新（删除不存在的项、更新现有项、添加新增项）
    /// </summary>
    /// <typeparam name="T">列表中实体的类型</typeparam>
    /// <param name="existingList">现有实体的列表（从数据库查询的）</param>
    /// <param name="incomingList">传入的待更新列表</param>
    /// <param name="getId">获取实体主键的方法（用于匹配）</param>
    private void UpdateChildList<T>(
    List<T> existingList,
    List<T> incomingList,
    Func<T, int?> getId,
    Func<T, bool> isNewItem // 新增：判断传入项是否为“新增”（如 Id 为空）
) where T : class, new()
    {
        // 1. 删除现有列表中“传入列表不存在”的项（逻辑不变）
        var itemsToDelete = existingList
            .Where(existing => !incomingList.Any(incoming =>
                Equals(getId(existing), getId(incoming))
            ))
            .ToList();
        foreach (var item in itemsToDelete)
        {
            existingList.Remove(item);
        }

        // 2. 处理传入的项
        foreach (var incomingItem in incomingList)
        {
            var incomingId = getId(incomingItem);
            var existingItem = existingList
                .FirstOrDefault(item => Equals(getId(item), incomingId));

            // 核心修改：如果是新增项（Id 为空），即使 existingItem 存在（也是新增的空 Id 项），也强制新建
            if (existingItem != null && !isNewItem(incomingItem))
            {
                // 现有项且传入项不是新增 → 更新
                _dbContext.Entry(existingItem).CurrentValues.SetValues(incomingItem);
            }
            else
            {
                // 传入项是新增，或没有匹配的现有项 → 新建
                var newItem = new T();
                _dbContext.Entry(newItem).CurrentValues.SetValues(incomingItem);
                existingList.Add(newItem);
            }
        }
    }

    public async Task<bool> DeletePDZ(string PDZId, CancellationToken cancellationToken = default)
    {
        var pdz = await _dbContext.Set<PlugDataZone>().FindAsync(new object[] { PDZId }, cancellationToken);
        if (pdz == null)
        {
            return false;
        }
        _dbContext.Set<PlugDataZone>().Remove(pdz);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteByFilter(PDZFilter filter, CancellationToken cancellationToken = default)
    {
        var pdz = await _dbContext.Set<PlugDataZone>().Include(p => p.PDZVariables)
            .Where(p => p.PDZId.Contains(filter.PlugDefinitionId))
            .ToListAsync(cancellationToken);
        if (pdz == null)
        {
            return false;
        }
        _dbContext.Set<PlugDataZone>().RemoveRange(pdz);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<PlugDataZone>?> GetAllPdz(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PlugDataZone>()
            .Include(p => p.PDZVariables)
            .Include(p => p.PlugDatas)
            .Include(p => p.PlugVariableDatas)
            .Include(p => p.PlugStatusDatas)
            .Include(p => p.ActionDatas)
            .Include(p => p.ActionVariableDatas)
            .Include(p => p.FlowchartDatas)
            .Include(p => p.DataFlowDatas)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    }

    public async Task<PlugDataZone?> GetByFilter(PDZFilter filter, CancellationToken cancellationToken = default)
    {
        // 开始构建查询
        var query = _dbContext.Set<PlugDataZone>()
            .Include(p => p.PDZVariables)
            .AsNoTracking();

        // 动态添加过滤条件
        if (!string.IsNullOrEmpty(filter.PDZId))
        {
            query = query.Where(p => p.PDZId == filter.PDZId);
        }

        if (!string.IsNullOrEmpty(filter.Type))
        {
            query = query.Where(p => p.Type == filter.Type);
        }

        if (!string.IsNullOrEmpty(filter.JobDefinitionId))
        {
            query = query.Where(p => p.JobDefinitionId == filter.JobDefinitionId);
        }

        if (!string.IsNullOrEmpty(filter.PlugDefinitionId))
        {
            query = query.Where(p => p.PlugDefinitionId == filter.PlugDefinitionId);
        }

        if (!string.IsNullOrEmpty(filter.UserName))
        {
            query = query.Where(p => p.UserName == filter.UserName);
        }

        if (!string.IsNullOrEmpty(filter.WorkPath))
        {
            query = query.Where(p => p.PDZWorkPath == filter.WorkPath);
        }

        // 执行查询
        var pdz = await query.FirstOrDefaultAsync(cancellationToken);

        if (pdz == null)
        {
            Console.WriteLine($"PDZ not found with filter: {JsonSerializer.Serialize(filter)}");
        }

        return pdz;
    }

    public async Task<PlugDataZone?> GetPdzById(int Id, CancellationToken cancellationToken = default)
    {
        var pdz = await _dbContext.Set<PlugDataZone>()
            .Include(p => p.PDZVariables)
            .Include(p => p.PlugDatas)
            .Include(p => p.PlugVariableDatas)
            .Include(p => p.PlugStatusDatas)
            .Include(p => p.ActionDatas)
            .Include(p => p.ActionVariableDatas)
            .Include(p => p.FlowchartDatas)
            .Include(p => p.DataFlowDatas)
            .AsNoTracking()
            .Where(p => p.Id == Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (pdz == null)
        {
            //Log.Information($"PDZ with ID {PDZId} not found.");
            return null;
        }
        return pdz;

    }


    public async Task<PlugDataZone?> GetByPDZId(string PDZId, CancellationToken cancellationToken = default)
    {
        var pdz = await _dbContext.Set<PlugDataZone>()
            .Include(p => p.PDZVariables)
            .Include(p => p.PlugDatas)
            .Include(p => p.PlugVariableDatas)
            .Include(p => p.PlugStatusDatas)
            .Include(p => p.ActionDatas)
            .Include(p => p.ActionVariableDatas)
            .Include(p => p.FlowchartDatas)
            .Include(p => p.DataFlowDatas)
            .AsNoTracking()
            .Where(p => p.PDZId == PDZId)
            .FirstOrDefaultAsync(cancellationToken);
        if (pdz == null)
        {
            //Log.Information($"PDZ with ID {PDZId} not found.");
            return null;
        }
        return pdz;

    }
}

