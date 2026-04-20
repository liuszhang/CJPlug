using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public partial class PlugManageService : IPlugManageService
{

    public async Task<IEnumerable<PlugAction>> GetAllPlugActionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PlugAction>()
            .Include(p => p.PlugVariables)
            .ToListAsync();
    }

    public async Task<PlugAction> CreatePlugActionAsync(PlugAction request, CancellationToken cancellationToken = default)
    {
        // 开始事务
        using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var toolItem = request;
                //var newRelation = new PlugToPlugAction
                //{
                //    PlugId = toolItem.ParentPlugId,
                //    PlugDefinitionId = toolItem.ParentPlugDefinitionId,
                //    //创建前还没有id号
                //    //PlugActionId = toolItem.Id
                //    PlugActionDefinitionId = toolItem.DefinitionId
                //};

                // 添加到PlugActions表
                _dbContext.Set<PlugAction>().Add(toolItem);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // 添加到PlugToPlugActions表
                //_dbContext.PlugToPlugActions.Add(newRelation);
                //await _dbContext.SaveChangesAsync(cancellationToken);

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

    public async Task<PlugAction?> UpdatePlugActionAsync(PlugAction request)
    {
        var item = await _dbContext.Set<PlugAction>()
            .Include(p => p.PlugVariables)
            .FirstOrDefaultAsync(t => t.Id == request.Id);
        if (item == null)
        {
            return null;
        }

        //Console.WriteLine("---------------------------------");
        //Console.WriteLine(JsonSerializer.Serialize(request.PlugVariables));

        item.Name = request.Name;
        item.Type = request.Type;
        item.Category = request.Category;
        item.Value = request.Value;
        item.PlugTypeKey = request.PlugTypeKey;
        item.ShowInPlugLibrary = request.ShowInPlugLibrary;
        item.PlugSettingsJson = request.PlugSettingsJson;

        Console.WriteLine(JsonSerializer.Serialize(item));
        // 保存所有更改
        await _dbContext.SaveChangesAsync();

        return item;

    }

    public async Task<bool> DeletePlugActionAsync(int id)
    {

        try
        {
            var item = await _dbContext.Set<PlugAction>().FindAsync(id);
            if (item == null)
            {
                return false;
            }
            //await DeleteRealationsAsync(null, item.DefinitionId);
            //Console.WriteLine("delete Realations");
            _dbContext.Set<PlugAction>().Remove(item);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine("delete PlugAction done");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }


    }

    public async Task<IEnumerable<Plug>?> GetPlugActionsByPlugDefinitionIdAsync(string? DefinitionId, CancellationToken cancellationToken = default)
    {
        var actions = await _dbContext.Set<Plug>()
            .Where(p => p.Tag == DefinitionId + "Action")
            .Include(p => p.PlugVariables)
            .ToListAsync();
        return actions;
    }
    public async Task<IEnumerable<PlugAction>?> GetPlugActionsByPlugIdAsync(int? Id, CancellationToken cancellationToken = default)
    {
        var actionIdList = await _dbContext.Set<PlugToPlugAction>().Where(p => p.PlugId == Id).ToListAsync();
        //Console.WriteLine(JsonSerializer.Serialize(actionIdList));
        var actions = new List<PlugAction>();
        foreach (var actionId in actionIdList)
        {
            actions.Add(await _dbContext.Set<PlugAction>().FirstOrDefaultAsync(a => a.DefinitionId == actionId.PlugActionDefinitionId));
        }
        return actions;
    }

    public async Task<PlugAction?> GetPlugActionByIdAsync(int? Id, CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.Set<PlugAction>().FirstOrDefaultAsync(a => a.Id == Id);        
        return action;
    }


    public async Task<string?> GetExecuteString(Plug toolItem, PlugAction toolActionItem)
    {
        var executeString = toolItem.ToolVersionPath??toolItem.Name;
        executeString += " ";
        executeString += toolActionItem.TargetLibPath ?? toolActionItem.TargetLib;
        executeString += " ";
        executeString += toolActionItem.LibFunctionName;
        executeString += " ";
        executeString += toolActionItem.FunctionParameters;
        foreach (var v in toolActionItem.PlugVariables)
        {
            if (v.IsInput)
            {
                executeString += " ";
                executeString += v.Value ?? v.DefaultValue;
            }            
        }
        Console.WriteLine($"{executeString}");
        return executeString;
        
    }

}

