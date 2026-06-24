using CJ.Plug.LlmConfigModel.Models;

namespace CJ.Plug.LlmConfigApi.Contracts;

public interface ILlmConfigService
{
    // 供应商 CRUD
    Task<IEnumerable<LlmProvider>> GetAllProvidersAsync(CancellationToken ct = default);
    Task<LlmProvider?> GetProviderByIdAsync(int id, CancellationToken ct = default);
    Task<LlmProvider?> CreateProviderAsync(LlmProvider provider, CancellationToken ct = default);
    Task<LlmProvider?> UpdateProviderAsync(LlmProvider provider, CancellationToken ct = default);
    Task<bool> DeleteProviderAsync(int id, CancellationToken ct = default);

    // 模型配置 CRUD
    Task<IEnumerable<LlmModelConfig>> GetAllModelConfigsAsync(CancellationToken ct = default);
    Task<IEnumerable<LlmModelConfig>> GetModelConfigsByProviderAsync(int providerId, CancellationToken ct = default);
    Task<LlmModelConfig?> GetModelConfigByIdAsync(int id, CancellationToken ct = default);
    Task<LlmModelConfig?> CreateModelConfigAsync(LlmModelConfig config, CancellationToken ct = default);
    Task<LlmModelConfig?> UpdateModelConfigAsync(LlmModelConfig config, CancellationToken ct = default);
    Task<bool> DeleteModelConfigAsync(int id, CancellationToken ct = default);

    // 获取默认模型信息（含供应商）
    Task<(LlmProvider? Provider, LlmModelConfig? Model)> GetDefaultModelInfoAsync(CancellationToken ct = default);

    // 设置默认模型
    Task<bool> SetDefaultModelAsync(int modelConfigId, CancellationToken ct = default);

    // 测试连接
    Task<(bool Success, string Message)> TestConnectionAsync(int modelConfigId, CancellationToken ct = default);

    // MCP Server 配置 CRUD
    Task<List<McpServerConfig>> GetMcpServerConfigsAsync(CancellationToken ct = default);
    Task<McpServerConfig?> CreateMcpServerConfigAsync(McpServerConfig config, CancellationToken ct = default);
    Task<McpServerConfig?> UpdateMcpServerConfigAsync(McpServerConfig config, CancellationToken ct = default);
    Task<bool> DeleteMcpServerConfigAsync(int id, CancellationToken ct = default);
}
