using CJ.Plug.MCPToolsManageApi.Contracts;
using CJ.Plug.Models.MCPTools;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Services;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.MCPToolsManageApi.Services
{
public class MCPToolsManageService : BaseRepositoryService<MCPTool, int>, IMCPToolsManageService
{
    public MCPToolsManageService(MainDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<MCPTool>> GetActiveToolsAsync()
    {
        var allTools = await base.GetAllAsync();
        return allTools?.Where(t => t.IsEnabled == true) ?? Enumerable.Empty<MCPTool>();
    }

    /// <summary>
    /// 发布工作流为 MCP Tool：保存 Tool 记录并创建 Use 类型 PDZ 作为参数模板
    /// </summary>
    public async Task<MCPTool> PublishToolAsync(MCPTool tool)
    {
        // 1. 保存 MCPTool 记录
        var saved = await base.CreateAsync(tool);

        if (string.IsNullOrEmpty(tool.SourcePlugId))
            return saved;

        // 2. 查找对应的 Plug 定义
        var plug = await _dbContext.Set<global::CJ.Plug.Models.Plug.Plug>()
            .Include(p => p.PlugVariables)
            .FirstOrDefaultAsync(p => p.DefinitionId == tool.SourcePlugId);

        if (plug == null)
            return saved;

        // 3. 创建或更新 Use 类型 PDZ（和用户无关，作为参数模板）
        var usePDZId = "MCP_Use_" + tool.SourcePlugId;
        var existingPDZ = await _dbContext.Set<PlugDataZone>()
            .Include(p => p.PlugVariableDatas)
            .FirstOrDefaultAsync(p => p.PDZId == usePDZId && p.Type == PDZTypeEnum.Use0.ToString());

        var pdz = existingPDZ ?? new PlugDataZone
        {
            PDZId = usePDZId,
            Type = PDZTypeEnum.Use0.ToString(),
            PlugDefinitionId = tool.SourcePlugId,
            UserName = "MCP_System",
        };

        // 从 Plug 定义同步变量到 PDZ
        pdz.SyncVariablesFromPlug(plug);

        if (existingPDZ != null)
            _dbContext.Update(pdz);
        else
            _dbContext.Add(pdz);

        await _dbContext.SaveChangesAsync();
        return saved;
    }

    /// <summary>
    /// 获取所有已发布为 MCP Tool 的工作流及其入口参数。
    /// McpServer 启动时调用此方法动态注册 Tool。
    /// </summary>
    public async Task<List<PublishedWorkflowDto>> GetPublishedWorkflowsAsync()
    {
        var tools = (await GetActiveToolsAsync()).ToList();
        if (tools.Count == 0)
            return new List<PublishedWorkflowDto>();

        var result = new List<PublishedWorkflowDto>();
        var sourcePlugIds = tools.Select(t => t.SourcePlugId).Where(id => !string.IsNullOrEmpty(id)).ToList();

        // 查询 Use 类型 PDZ（MCP Tool 发布时创建的参数模板），Eager Load PlugVariableDatas
        var useType = PDZTypeEnum.Use0.ToString();
        var pdZs = await _dbContext.Set<PlugDataZone>()
            .Where(p => sourcePlugIds.Contains(p.PlugDefinitionId) && p.Type == useType)
            .Include(p => p.PlugVariableDatas)
            .ToListAsync();
        foreach (var tool in tools)
        {
            if (string.IsNullOrEmpty(tool.SourcePlugId))
                continue;

            var dto = new PublishedWorkflowDto
            {
                WorkflowDefinitionId = tool.SourcePlugId,
                Name = tool.Name,
                Description = tool.Description,
            };

            // 从 Use PDZ 提取入口变量
            var pdZ = pdZs.FirstOrDefault(p => p.PlugDefinitionId == tool.SourcePlugId);
            if (pdZ?.PlugVariableDatas != null && pdZ.PlugVariableDatas.Count > 0)
            {
                dto.EntryVariables = pdZ.PlugVariableDatas
                    .Where(v => v.IsInput && v.IsBrowsable != false)
                    .Select(v => ToEntryDto(v))
                    .ToList();
            }

            result.Add(dto);
        }

        return result;
    }

    private static EntryVariableDto ToEntryDto(BaseVariable v)
    {
        return new EntryVariableDto
        {
            Name = v.Name,
            DisplayName = v.DisplayName,
            Description = v.Description,
            Type = v.Type,
            IsRequired = v.IsRequired == true,
            IsArray = v.IsArray,
            DefaultValue = v.DefaultValue,
        };
    }
}
}
