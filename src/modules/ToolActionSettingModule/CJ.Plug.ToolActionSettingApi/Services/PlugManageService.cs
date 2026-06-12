using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;
using Serilog;

public partial class PlugManageService : IPlugManageService
{
    private readonly MainDbContext _dbContext;

    public PlugManageService(MainDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreatedAsync();
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
        return await _dbContext.Set<Plug>()
            .Include(p=>p.PlugVariables)
            .ToListAsync();
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

