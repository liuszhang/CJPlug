
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Process = CJ.Plug.Models.PlugProcess.Process;

public class ProcessManageService : BaseRepositoryService<Process, int>, IProcessManageService
{
    private readonly MainDbContext _ProcessDbContext;
    public ProcessManageService(MainDbContext dbContext) : base(dbContext)
    {
        _ProcessDbContext = dbContext;
    }

    public async Task<IEnumerable<Process>> GetAllWorkflowsAsync(string? userName = null, CancellationToken cancellationToken = default)
    {
        //await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        var query = _ProcessDbContext.Set<Process>().AsQueryable();

        // admin 用户可以看到所有流程，普通用户仅看到自己创建的流程
        if (!string.IsNullOrEmpty(userName) && !string.Equals(userName, "admin", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Creater == userName);
        }

        return await query.Include(p => p.PlugVariables).ToListAsync(cancellationToken);
    }
    public async Task<Process> CreateWorkflowAsync(Process request, CancellationToken cancellationToken = default)
    {
        request.CreatedAt = DateTime.Now.ToString();
        request.UpdatedAt = DateTime.Now.ToString();
        request.WorkPath = Path.Combine(
            request.Creater,
            FileFolderType.Design.ToString(),
            request.DefinitionId);

        //request.WorkPath = Path.Combine(GlobalData.MainFileServerPathRoot, request.Creater??"null", request.DefinitionId);
        //Console.WriteLine(request.WorkPath);
        _ProcessDbContext.Set<Process>().Add(request);
        await _ProcessDbContext.SaveChangesAsync();
        return request;

    }

    public async Task<bool> DeleteWorkflowAsync(int id)
    {
        var workflow = await _ProcessDbContext.Set<Process>().FindAsync(id);
        if (workflow == null)
        {
            return false;
        }

        _ProcessDbContext.Set<Process>().Remove(workflow);
        await _ProcessDbContext.SaveChangesAsync();
        return true;
    }

    public async Task<JsonObject> GetWorkflowJsonAsync(int? Id, CancellationToken cancellationToken = default)
    {
        //Console.WriteLine(">>>>>>>>>>>>api accept id is:"+Id);
        //await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        var workflow = await _ProcessDbContext.Set<Process>().FindAsync(Id);
        if (workflow == null)
        {
            return null;
        }

        // 将字符串转换为 JsonObject
        JsonObject jsonObject = JsonSerializer.Deserialize<JsonObject>(workflow.ActivityJsonData);
        //Console.WriteLine(">>>>>>>>>>>>find json data is:" + jsonObject);
        return jsonObject;

    }

    public async Task<Process?> GetWorkflowById(int id)
    {
        var workflow = await _ProcessDbContext.Set<Process>()
            .Include(p => p.PlugVariables)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (workflow == null)
        {
            return null;
        }
        //Console.WriteLine(">>>>>>>>>>>>find workflow data is:" + JsonSerializer.Serialize(workflow));
        return workflow;
    }

}


