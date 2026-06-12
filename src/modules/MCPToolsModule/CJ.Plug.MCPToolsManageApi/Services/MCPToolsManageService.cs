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
        private readonly IMcpToolChangeNotifier? _notifier;
        private readonly CapabilityRegistry? _capabilityRegistry;

        public MCPToolsManageService(MainDbContext dbContext, IMcpToolChangeNotifier? notifier = null, CapabilityRegistry? capabilityRegistry = null) : base(dbContext)
        {
            _notifier = notifier;
            _capabilityRegistry = capabilityRegistry;
        }

        public override async Task<MCPTool> CreateAsync(MCPTool entity, CancellationToken cancellationToken = default)
        {
            var result = await base.CreateAsync(entity, cancellationToken);
            var notifyId = result.ToolId ?? result.SourcePlugId;
            if (_notifier != null && !string.IsNullOrEmpty(notifyId))
                await _notifier.NotifyAsync(notifyId, "published", cancellationToken);
            return result;
        }

        public override async Task<MCPTool?> UpdateAsync(MCPTool entity, CancellationToken cancellationToken = default)
        {
            var result = await base.UpdateAsync(entity, cancellationToken);
            var notifyId = result?.ToolId ?? result?.SourcePlugId;
            if (_notifier != null && result != null && !string.IsNullOrEmpty(notifyId))
                await _notifier.NotifyAsync(notifyId, "updated", cancellationToken);
            return result;
        }

        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            var toolId = entity?.ToolId ?? entity?.SourcePlugId;
            var result = await base.DeleteAsync(id, cancellationToken);
            if (result && _notifier != null && !string.IsNullOrEmpty(toolId))
                await _notifier.NotifyAsync(toolId, "deleted", cancellationToken);
            return result;
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

            // 通知 McpServer 刷新工具列表
            var notifyId = saved.ToolId ?? saved.SourcePlugId;
            if (_notifier != null && !string.IsNullOrEmpty(notifyId))
                await _notifier.NotifyAsync(notifyId, "published");

            if (string.IsNullOrEmpty(tool.SourcePlugId))
                return saved;

            // 2. 查找对应的 Plug 定义
            //var plug = await _dbContext.Set<global::CJ.Plug.Models.Plug.Plug>()
            //    .Include(p => p.PlugVariables)
            //    .FirstOrDefaultAsync(p => p.DefinitionId == tool.SourcePlugId);

            //if (plug == null)
            //    return saved;

            // 2. 查找对应的 PDZ 定义
            var designPDZ = await _dbContext.Set<PlugDataZone>()
                .Include(p => p.PlugVariableDatas)
                .FirstOrDefaultAsync(p => p.PlugDefinitionId == tool.SourcePlugId && p.Type == PDZTypeEnum.Desi.ToString());

            if (designPDZ == null)
            {
                Console.WriteLine($"Warning: No design PDZ found for PlugDefinitionId {tool.SourcePlugId}. Skipping PDZ creation.");
                return saved;
            }

            // 3. 使用 CopyPDZ 完整复制 Desi PDZ → Use0 PDZ（作为参数模板）
            //    CopyPDZ 会完整保留 IsInput、IsBrowsable、IsRequired 等元数据，
            //    确保 McpServer 通过 GetPublishedWorkflowsAsync 能正确读取入口参数
            var usePDZId = "MCP_Use0_" + tool.SourcePlugId;

            // 先删除旧的 Use0 PDZ（如果存在），然后基于 Desi PDZ 创建新的副本
            var existingPDZ = await _dbContext.Set<PlugDataZone>()
                .FirstOrDefaultAsync(p => p.PDZId == usePDZId && p.Type == PDZTypeEnum.Use0.ToString());
            if (existingPDZ != null)
            {
                _dbContext.Set<PlugDataZone>().Remove(existingPDZ);
                await _dbContext.SaveChangesAsync(); // 先保存删除，避免 PDZId 冲突
            }

            var pdz = designPDZ.CopyPDZ("MCP_System", PDZTypeEnum.Use0.ToString(), tool.SourcePlugId);
            pdz.PDZId = usePDZId; // 使用固定的 MCP 参数模板 PDZ ID

            // 发布为 MCP Tool 时，将所有可见参数标记为输入参数
            if (pdz.PlugVariableDatas != null)
            {
                foreach (var v in pdz.PlugVariableDatas)
                {
                    if (v.IsBrowsable != false)
                    {
                        v.IsInput = true;
                    }
                }
            }

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
                    ToolType = tool.ToolType ?? "Workflow",
                };

                // 从 Use PDZ 提取入口变量
                var pdZ = pdZs.FirstOrDefault(p => p.PlugDefinitionId == tool.SourcePlugId);
                if (pdZ?.PlugVariableDatas != null && pdZ.PlugVariableDatas.Count > 0)
                {
                    Console.WriteLine($"[GetPublishedWorkflowsAsync] Tool={tool.Name}, PDZ={pdZ.PDZId}, Total variables={pdZ.PlugVariableDatas.Count}");
                    foreach (var v in pdZ.PlugVariableDatas)
                    {
                        Console.WriteLine($"[GetPublishedWorkflowsAsync]   Var: Name={v.Name}, IsInput={v.IsInput}, IsBrowsable={v.IsBrowsable}, Type={v.Type}");
                    }

                    dto.EntryVariables = pdZ.PlugVariableDatas
                        .Where(v => v.IsInput && v.IsBrowsable != false && v.PlugDefinitionId == tool.SourcePlugId)
                        .Select(v => ToEntryDto(v))
                        .ToList();

                    Console.WriteLine($"[GetPublishedWorkflowsAsync] Tool={tool.Name}, EntryVariables after filter={dto.EntryVariables.Count}");
                }
                else
                {
                    Console.WriteLine($"[GetPublishedWorkflowsAsync] Tool={tool.Name}, No PlugVariableDatas found. pdZ exists={pdZ != null}, count={pdZ?.PlugVariableDatas?.Count ?? -1}");
                }

                // 回退：如果 Use PDZ 中没有找到入口参数，从 Plug 定义中获取
                if (dto.EntryVariables.Count == 0)
                {
                    try
                    {
                        var plug = await _dbContext.Set<CJ.Plug.Models.Plug.Plug>()
                            .Include(p => p.PlugVariables)
                            .FirstOrDefaultAsync(p => p.DefinitionId == tool.SourcePlugId);
                        if (plug?.PlugVariables != null && plug.PlugVariables.Count > 0)
                        {
                            Console.WriteLine($"[GetPublishedWorkflowsAsync] Fallback to Plug definition: Tool={tool.Name}, PlugVariables={plug.PlugVariables.Count}");
                            dto.EntryVariables = plug.PlugVariables
                                .Where(v => v.IsBrowsable != false)
                                .Select(v => ToEntryDto(v))
                                .ToList();
                            Console.WriteLine($"[GetPublishedWorkflowsAsync] Fallback: Tool={tool.Name}, EntryVariables={dto.EntryVariables.Count}");
                        }

                        // CapabilityRegistry fallback for Plugin tools（CMD插头等）
                        if (dto.EntryVariables.Count == 0 && tool.ToolType == "Plugin"
                            && _capabilityRegistry != null && plug != null && !string.IsNullOrEmpty(plug.PlugTypeKey))
                        {
                            var capability = _capabilityRegistry.Get(plug.PlugTypeKey);
                            if (capability != null && capability.Inputs.Count > 0)
                            {
                                Console.WriteLine($"[GetPublishedWorkflowsAsync] CapabilityRegistry: Tool={tool.Name}, PlugTypeKey={plug.PlugTypeKey}, Inputs={capability.Inputs.Count}");
                                dto.EntryVariables = capability.Inputs
                                    .Select(p => new EntryVariableDto
                                    {
                                        Name = p.Name,
                                        DisplayName = p.Name,
                                        Description = p.Description,
                                        Type = p.Type,
                                        IsRequired = p.IsRequired,
                                        IsArray = p.IsArray,
                                        Value = p.Value,
                                    })
                                    .ToList();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GetPublishedWorkflowsAsync] Fallback failed for Tool={tool.Name}: {ex.Message}");
                    }
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
                Value = v.Value,
            };
        }
    }
}
