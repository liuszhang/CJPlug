using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;

public partial class PlugManageService : IPlugManageService
{
    private readonly MainDbContext _dbContext;

    public PlugManageService(MainDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreated();
        //MigratePlugSchema();
    }

    public async Task<Plug> CreatePlugAsync(Plug request, CancellationToken cancellationToken = default)
    {
        //var toolItem = JsonSerializer.Deserialize<ToolItem>(request);
        if(string.IsNullOrEmpty(request.ParentPlugDefinitionId))
        {
            var creator = string.IsNullOrEmpty(request.Creater) ? "Default" : request.Creater;
            request.WorkPath = Path.Combine(
                "Plugs",
                creator,
                request.DefinitionId);
        }
        else
        {
            var parentWorkPath = _dbContext.Set<Plug>()
                .Where(p => p.DefinitionId == request.ParentPlugDefinitionId)
                .Select(p => p.WorkPath)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(parentWorkPath))
            {
                throw new InvalidOperationException($"父插头 '{request.ParentPlugDefinitionId}' 不存在或 WorkPath 为空，无法创建子插头。");
            }
            request.WorkPath = Path.Combine(
                parentWorkPath,
                request.DefinitionId);
        }
            

        var toolItem = (request);        
        _dbContext.Set<Plug>().Add(toolItem);
        await _dbContext.SaveChangesAsync();
        return toolItem;
    }
    

    public async Task<bool> DeletePlugAsync(int id)
    {
        // 开始事务
        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                var item = await _dbContext.Set<Plug>().FindAsync(id);
                if (item == null)
                {
                    return false;
                }

                _dbContext.Set<Plug>().Remove(item);
                //还需要删除关系表中的数据
                var relations = await _dbContext.Set<PlugToPlugAction>().Where(r => r.PlugId == id).ToListAsync();
                foreach (var relation in relations)
                {
                    _dbContext.Set<PlugToPlugAction>().Remove(relation);
                    var action = await _dbContext.Set<PlugAction>().Where(a => a.DefinitionId == relation.PlugDefinitionId).FirstOrDefaultAsync();
                    if (action != null && action.IsRootPlug == false)
                    {
                        _dbContext.Set<PlugAction>().Remove(action);
                    }
                }

                await _dbContext.SaveChangesAsync(); 
                
                // 提交事务
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                // 发生异常时回滚事务
                await transaction.RollbackAsync();
                throw; // 或者根据需要处理异常
            }
        }

        
    }

    public async Task<IEnumerable<Plug>> GetAllPlugsAsync(CancellationToken cancellationToken = default)
    {
        var plugs = await _dbContext.Set<Plug>()
            .Include(p=>p.PlugVariables)
            .ToListAsync();
        Log.Information("[GetAllPlugsAsync] 返回 {Count} 个插头，前5条: {Plugs}",
            plugs.Count,
            plugs.Take(5).Select(p => new { p.Id, p.SortOrder, p.Name }));
        return plugs;
    }

    public async Task<IEnumerable<Plug>?> GetChildPlugsAsync(string DefinitionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Plug>()
            .Where(p => p.ParentPlugDefinitionId == DefinitionId)
            .ToListAsync();
    }

    public async Task<Plug?> GetPlugById(int id)
    {
        var item = await _dbContext.Set<Plug>()
            .Include(p=>p.PlugVariables)
            //.Include(p => p.PlugActions)
            //.ThenInclude(ta => ta.ToolActionVariables)  // 加载 ToolActionVariables
            .FirstOrDefaultAsync(p => p.Id == id);
        if (item == null)
        {
            return null;
        }        
        return item;
    }

    public async Task<Plug?> GetParentPlugById(int? id)
    {
        var actionItem = await _dbContext.Set<PlugAction>()
            .FirstOrDefaultAsync(a => a.Id == id);
        var item=await _dbContext.Set<Plug>()
            .Include(p => p.PlugVariables)
            //.Include(p => p.PlugActions)
            .FirstOrDefaultAsync(p => p.Id == actionItem.ParentPlugId);
        if (item == null)
        {
            return null;
        }
        return item;
    }

    public async Task<Plug?> GetPlugByDefinitionId(string definiitonId)
    {
        var item = await _dbContext.Set<Plug>()
            .Include(v=>v.PlugVariables)
            //.Include(p => p.PlugActions)
            //.ThenInclude(ta => ta.ToolActionVariables)  // 加载 ToolActionVariables
            .FirstOrDefaultAsync(p => p.DefinitionId == definiitonId);
        if (item == null)
        {
            return null;
        }
        return item;
    }
    public async Task<Plug?> GetPlugByTypeName(string typeName)
    {
        var item = await _dbContext.Set<Plug>()
            .Include (v=>v.PlugVariables)
            .Where(p=>
            p.CreateType==PlugCreateTypeEnum.RootPlug.ToString()||
            p.CreateType == PlugCreateTypeEnum.RootAdminPlug.ToString() ||
            p.CreateType == PlugCreateTypeEnum.SystemInitPlug.ToString())
            .Where(p=>EF.Functions.Like(p.PlugTypeKey, typeName))
            .FirstOrDefaultAsync();
        if (item == null)
        {
            Console.WriteLine($"未找到类型为{typeName}的插头");
            return null;
        }
        return item;
    }

    public async Task<Plug?> GetPlugByNameAsync(string name)
    {
        var item = await _dbContext.Set<Plug>()
            .Include(v => v.PlugVariables)
            .Where(p =>
                p.CreateType == PlugCreateTypeEnum.RootPlug.ToString() ||
                p.CreateType == PlugCreateTypeEnum.RootAdminPlug.ToString() ||
                p.CreateType == PlugCreateTypeEnum.SystemInitPlug.ToString())
            .Where(p => p.Name == name)
            .FirstOrDefaultAsync();
        return item;
    }

    public async Task<Plug?> UpdatePlugAsync(int id, Plug request)
    {
        var item = await _dbContext.Set<Plug>()
            .Include(p => p.PlugVariables)
            //.Include(a => a.PlugActions)
            //.ThenInclude(ta => ta.ToolActionVariables)// 确保加载子数据
            .FirstOrDefaultAsync(t => t.Id == id);
        if (item == null)
        {
            return null;
        }

        //Console.WriteLine("---------------------------------");
        //Console.WriteLine(JsonSerializer.Serialize(request.PlugVariables));
        

        item.Name = request.Name;
        item.Category = request.Category;
        item.Description = request.Description;
        item.GroupName = request.GroupName;
        item.Value = request.Value;     
        item.RealValuePath = request.RealValuePath;
        item.ToolVersionPath = request.ToolVersionPath;
        item.ToolVersion = request.ToolVersion;
        item.ToolId = request.ToolId;
        item.ToolName = request.ToolName;
        item.ToolDisplayName = request.ToolDisplayName;
        item.ToolCommandLineShema = request.ToolCommandLineShema;
        //item.IsRootPlug = request.IsRootPlug;
        //item.IsCustomeToolPlug = request.IsCustomeToolPlug;
        //item.IsSystemInitPlug = request.IsSystemInitPlug;
        item.CreateType = request.CreateType;
        item.Icon = request.Icon;
        item.PlugTypeKey = request.PlugTypeKey;
        item.ShowInPlugLibrary= request.ShowInPlugLibrary;
        item.ToolVersions = request.ToolVersions;
        item.PlugSettingsJson = request.PlugSettingsJson;
        item.ActivityJsonData = request.ActivityJsonData;
        item.OnlyExecuteAction = request.OnlyExecuteAction;
        item.IsContainerPlug = request.IsContainerPlug;
        item.GuiJsonData = request.GuiJsonData;


        // 清除现有的 PlugVariables
        //var existingPlugVariables = item.PlugVariables.ToList();
        //foreach (var toolAction in existingPlugVariables)
        //{
        //    _dbContext.Entry(toolAction).State = EntityState.Deleted;
        //}
        //item.PlugVariables.Clear();
        // 阶段一：删除现有子项
        _dbContext.Set<PlugVariable>().RemoveRange(item.PlugVariables);
        // 立即提交删除操作（缩小事务范围）
        //item.PlugVariables.Clear();
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("---------------------delete all variables success--------------------");

        // 添加新的 PlugVariables
        foreach (var v in request.PlugVariables)
        {
            v.Id = null;
            item.PlugVariables.Add(v);
        }

        //Console.WriteLine(JsonSerializer.Serialize(item));
        // 保存所有更改
        await _dbContext.SaveChangesAsync();

        return item;

    }


    private void MigratePlugSchema()
    {
        try
        {
            var conn = _dbContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            // 检查 SortOrder 列是否存在
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(Plugs)";
            using var reader = cmd.ExecuteReader();
            bool hasSortOrder = false;
            while (reader.Read())
            {
                var colName = reader.GetString(1); // PRAGMA table_info 第2列为列名
                if (colName == "SortOrder")
                {
                    hasSortOrder = true;
                    break;
                }
            }
            reader.Close();

            if (!hasSortOrder)
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE Plugs ADD COLUMN SortOrder INTEGER";
                alterCmd.ExecuteNonQuery();
                Log.Information("[MigratePlugSchema] 已为 Plugs 表添加 SortOrder 列，数据库路径: {Path}", conn.DataSource);
            }
            else
            {
                Log.Information("[MigratePlugSchema] SortOrder 列已存在，跳过迁移。数据库路径: {Path}", conn.DataSource);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[MigratePlugSchema] Schema 迁移失败");
        }
    }

    public async Task BatchUpdateSortOrdersAsync(List<PlugSortOrderDto> sortOrders, CancellationToken cancellationToken = default)
    {
        if (sortOrders == null || sortOrders.Count == 0)
        {
            Log.Warning("[BatchUpdateSortOrders] 收到空的排序列表，跳过");
            return;
        }

        Log.Information("[BatchUpdateSortOrders] 收到 {Count} 个排序项: {@Orders}",
            sortOrders.Count, sortOrders.Select(s => new { s.Id, s.SortOrder }));

        var ids = sortOrders.Select(s => s.Id).ToList();
        var plugs = await _dbContext.Set<Plug>()
            .Where(p => p.Id != null && ids.Contains(p.Id.Value))
            .ToListAsync(cancellationToken);

        Log.Information("[BatchUpdateSortOrders] 查询到 {Found}/{Requested} 个插头",
            plugs.Count, ids.Count);

        int updated = 0;
        foreach (var plug in plugs)
        {
            var dto = sortOrders.FirstOrDefault(s => s.Id == plug.Id);
            if (dto != null)
            {
                plug.SortOrder = dto.SortOrder;
                updated++;
            }
        }

        try
        {
            var entryCount = _dbContext.ChangeTracker.Entries<Plug>()
                .Count(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified);
            Log.Information("[BatchUpdateSortOrders] 变更追踪: {Modified} 个实体 Modified，即将 SaveChanges", entryCount);

            await _dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[BatchUpdateSortOrders] SaveChanges 成功，更新了 {Updated} 个插头的 SortOrder", updated);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BatchUpdateSortOrders] SaveChanges 失败: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<PlugVariable>?> GetVariablesByDefinitionId(string definiitonId)
    {
        var item = await _dbContext.Set<Plug>()
            .Include(v => v.PlugVariables)
            .FirstOrDefaultAsync(p => p.DefinitionId == definiitonId);
        if (item == null)
        {
            return null;
        }
        List<PlugVariable> variables = new();
        foreach (var v in item.PlugVariables)
        {
            variables.Add(v);
        }

        return variables;
    }
}

