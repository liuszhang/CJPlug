using CJ.Plug.LlmConfigApi.Contracts;
using CJ.Plug.LlmConfigModel.Models;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CJ.Plug.LlmConfigApi.Services;

public class LlmConfigService : ILlmConfigService
{
    private readonly MainDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LlmConfigService> _logger;

    public LlmConfigService(MainDbContext dbContext, IHttpClientFactory httpClientFactory, ILogger<LlmConfigService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ---- Public Static Mapping Methods ----

    public static LlmProvider MapToDto(LlmProvider p) => new()
    {
        Id = p.Id, Name = p.Name, DisplayName = p.DisplayName,
        ApiBaseUrl = p.ApiBaseUrl,
        ApiKey = MaskApiKey(p.ApiKey),
        Description = p.Description,
        SortOrder = p.SortOrder, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
        ModelConfigs = p.ModelConfigs?.Select(MapModelConfigToDto).ToList() ?? new()
    };

    /// <summary>映射 Provider 用于内部调用（不脱敏 ApiKey），供 GetDefaultModelInfoAsync 等内部链路使用。</summary>
    private static LlmProvider MapProviderWithRawApiKey(LlmProvider p) => new()
    {
        Id = p.Id, Name = p.Name, DisplayName = p.DisplayName,
        ApiBaseUrl = p.ApiBaseUrl,
        ApiKey = p.ApiKey,
        Description = p.Description,
        SortOrder = p.SortOrder, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
        ModelConfigs = p.ModelConfigs?.Select(MapModelConfigToDto).ToList() ?? new()
    };

    public static LlmModelConfig MapModelConfigToDto(LlmModelConfig m) => new()
    {
        Id = m.Id, LlmProviderId = m.LlmProviderId, ModelName = m.ModelName,
        DisplayName = m.DisplayName, ModelType = m.ModelType,
        MaxTokens = m.MaxTokens, Temperature = m.Temperature,
        IsDefault = m.IsDefault,
        Description = m.Description, ExtraParams = m.ExtraParams,
        CreatedAt = m.CreatedAt, UpdatedAt = m.UpdatedAt
    };

    private static string? MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return null;
        if (apiKey.Length <= 8) return "****";
        return apiKey[..4] + "****" + apiKey[^4..];
    }

    // ---- Service Methods ----

    public async Task<IEnumerable<LlmProvider>> GetAllProvidersAsync(CancellationToken ct = default)
    {
        var providers = await _dbContext.Set<LlmProvider>()
            .Include(p => p.ModelConfigs)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        foreach (var p in providers)
        {
            var defaultModel = p.ModelConfigs?.FirstOrDefault(m => m.IsDefault);
            _logger.LogInformation("[GetAllProviders] Provider={Name} (Id={Id}), ModelConfigs 数={Count}, 默认模型={DefaultModel}",
                p.Name, p.Id, p.ModelConfigs?.Count ?? 0,
                defaultModel != null ? $"{defaultModel.ModelName}(IsDefault={defaultModel.IsDefault})" : "无");
        }

        return providers.Select(MapToDto);
    }

    public async Task<LlmProvider?> GetProviderByIdAsync(int id, CancellationToken ct = default)
    {
        var provider = await _dbContext.Set<LlmProvider>()
            .Include(p => p.ModelConfigs)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        return provider == null ? null : MapToDto(provider);
    }

    public async Task<LlmProvider?> CreateProviderAsync(LlmProvider provider, CancellationToken ct = default)
    {
        provider.CreatedAt = DateTime.UtcNow.ToLocalTime();
        provider.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        _dbContext.Set<LlmProvider>().Add(provider);
        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(provider);
    }

    public async Task<LlmProvider?> UpdateProviderAsync(LlmProvider provider, CancellationToken ct = default)
    {
        var existing = await _dbContext.Set<LlmProvider>().FirstOrDefaultAsync(p => p.Id == provider.Id, ct);
        if (existing == null) return null;

        existing.Name = provider.Name;
        existing.DisplayName = provider.DisplayName;
        existing.ApiBaseUrl = provider.ApiBaseUrl;
        // 只有当传入的 ApiKey 非空且不是掩码值时才更新
        if (!string.IsNullOrEmpty(provider.ApiKey) && !provider.ApiKey.Contains("****"))
            existing.ApiKey = provider.ApiKey;
        existing.Description = provider.Description;
        existing.SortOrder = provider.SortOrder;
        existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(existing);
    }

    public async Task<bool> DeleteProviderAsync(int id, CancellationToken ct = default)
    {
        var provider = await _dbContext.Set<LlmProvider>().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (provider == null) return false;
        _dbContext.Set<LlmProvider>().Remove(provider);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IEnumerable<LlmModelConfig>> GetAllModelConfigsAsync(CancellationToken ct = default)
    {
        var configs = await _dbContext.Set<LlmModelConfig>()
            .OrderBy(m => m.LlmProviderId)
            .ThenBy(m => m.ModelName)
            .ToListAsync(ct);
        return configs.Select(MapModelConfigToDto);
    }

    public async Task<IEnumerable<LlmModelConfig>> GetModelConfigsByProviderAsync(int providerId, CancellationToken ct = default)
    {
        var configs = await _dbContext.Set<LlmModelConfig>()
            .Where(m => m.LlmProviderId == providerId)
            .OrderBy(m => m.ModelName)
            .ToListAsync(ct);
        return configs.Select(MapModelConfigToDto);
    }

    public async Task<LlmModelConfig?> GetModelConfigByIdAsync(int id, CancellationToken ct = default)
    {
        var config = await _dbContext.Set<LlmModelConfig>().FirstOrDefaultAsync(m => m.Id == id, ct);
        return config == null ? null : MapModelConfigToDto(config);
    }

    public async Task<LlmModelConfig?> CreateModelConfigAsync(LlmModelConfig config, CancellationToken ct = default)
    {
        if (config.IsDefault)
            await ClearDefaultForTypeAsync(config.ModelType, ct);

        config.CreatedAt = DateTime.UtcNow.ToLocalTime();
        config.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        _dbContext.Set<LlmModelConfig>().Add(config);
        await _dbContext.SaveChangesAsync(ct);
        return MapModelConfigToDto(config);
    }

    public async Task<LlmModelConfig?> UpdateModelConfigAsync(LlmModelConfig config, CancellationToken ct = default)
    {
        var existing = await _dbContext.Set<LlmModelConfig>().FirstOrDefaultAsync(m => m.Id == config.Id, ct);
        if (existing == null) return null;

        if (config.IsDefault && !existing.IsDefault)
            await ClearDefaultForTypeAsync(config.ModelType, ct);

        existing.ModelName = config.ModelName;
        existing.DisplayName = config.DisplayName;
        existing.ModelType = config.ModelType;
        existing.MaxTokens = config.MaxTokens;
        existing.Temperature = config.Temperature;
        existing.IsDefault = config.IsDefault;
        existing.Description = config.Description;
        existing.ExtraParams = config.ExtraParams;
        existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        await _dbContext.SaveChangesAsync(ct);
        return MapModelConfigToDto(existing);
    }

    public async Task<bool> DeleteModelConfigAsync(int id, CancellationToken ct = default)
    {
        var config = await _dbContext.Set<LlmModelConfig>().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (config == null) return false;
        _dbContext.Set<LlmModelConfig>().Remove(config);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(LlmProvider? Provider, LlmModelConfig? Model)> GetDefaultModelInfoAsync(CancellationToken ct = default)
    {
        // 获取所有模型总数用于诊断
        var totalModels = await _dbContext.Set<LlmModelConfig>()
            .CountAsync(ct);
        _logger.LogInformation("GetDefaultModelInfo: total models in DB = {Count}", totalModels);

        // 优先找 IsDefault 的模型
        var defaultModel = await _dbContext.Set<LlmModelConfig>()
            .Where(m => m.IsDefault)
            .FirstOrDefaultAsync(ct);
        _logger.LogInformation("GetDefaultModelInfo: IsDefault query → {Found}", defaultModel != null);

        // 没有默认模型则取第一个
        if (defaultModel == null)
        {
            defaultModel = await _dbContext.Set<LlmModelConfig>()
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync(ct);
            _logger.LogInformation("GetDefaultModelInfo: fallback first-model query → {Found}", defaultModel != null);
        }

        if (defaultModel == null)
        {
            _logger.LogWarning("GetDefaultModelInfo: no model found at all");
            return (null, null);
        }

        var provider = await _dbContext.Set<LlmProvider>()
            .FirstOrDefaultAsync(p => p.Id == defaultModel.LlmProviderId, ct);

        if (provider == null)
        {
            _logger.LogWarning("GetDefaultModelInfo: provider not found for ProviderId={ProviderId}", defaultModel.LlmProviderId);
            return (null, null);
        }

        // 不脱敏 ApiKey — AskAI 等内部调用链路需要完整凭据来发起 LLM 请求
        var dto = MapProviderWithRawApiKey(provider);
        _logger.LogInformation("GetDefaultModelInfo: Provider={Name}, ApiKeyLen={KeyLen}, ApiBaseUrl={Url}, Model={Model}",
            dto.Name, dto.ApiKey?.Length ?? 0, dto.ApiBaseUrl, defaultModel.ModelName);
        return (dto, MapModelConfigToDto(defaultModel));
    }

    public async Task<bool> SetDefaultModelAsync(int modelConfigId, CancellationToken ct = default)
    {
        var config = await _dbContext.Set<LlmModelConfig>().FirstOrDefaultAsync(m => m.Id == modelConfigId, ct);
        if (config == null)
        {
            _logger.LogWarning("[SetDefaultModel] modelConfigId={Id} 未找到", modelConfigId);
            return false;
        }

        _logger.LogInformation("[SetDefaultModel] 找到模型 Id={Id}, ModelName={Name}, ModelType={Type}, LlmProviderId={ProvId}, 当前 IsDefault={Def}",
            config.Id, config.ModelName, config.ModelType, config.LlmProviderId, config.IsDefault);

        await ClearDefaultForTypeAsync(config.ModelType, ct);

        config.IsDefault = true;
        config.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        var rows = await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("[SetDefaultModel] SaveChanges 完成, 影响行数={Rows}, config.IsDefault={Def}", rows, config.IsDefault);
        return true;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(int modelConfigId, CancellationToken ct = default)
    {
        var config = await _dbContext.Set<LlmModelConfig>().FirstOrDefaultAsync(m => m.Id == modelConfigId, ct);
        if (config == null) return (false, "模型配置不存在");

        var provider = await _dbContext.Set<LlmProvider>().FirstOrDefaultAsync(p => p.Id == config.LlmProviderId, ct);
        if (provider == null) return (false, "供应商不存在");

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            if (!string.IsNullOrEmpty(provider.ApiKey))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);

            var endpoint = provider.ApiBaseUrl.TrimEnd('/') + "/chat/completions";
            var payload = new
            {
                model = config.ModelName,
                messages = new[] { new { role = "user", content = "Hi" } },
                max_tokens = 5,
                stream = false
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
                return (true, $"连接成功 ({(int)response.StatusCode})");

            return (false, $"请求失败: HTTP {(int)response.StatusCode} - {responseBody[..Math.Min(200, responseBody.Length)]}");
        }
        catch (TaskCanceledException)
        {
            return (false, "连接超时 (15s)");
        }
        catch (Exception ex)
        {
            return (false, $"连接失败: {ex.Message}");
        }
    }

    // ---- MCP Server 配置 ----


    public async Task<List<McpServerConfig>> GetMcpServerConfigsAsync(CancellationToken ct = default)
    {
        var configs = await _dbContext.Set<McpServerConfig>()
            .OrderBy(c => c.Id)
            .ToListAsync(ct);
        return configs.Select(MapMcpServerConfigToDto).ToList();
    }

    public async Task<McpServerConfig?> CreateMcpServerConfigAsync(McpServerConfig config, CancellationToken ct = default)
    {
        config.Id = 0;
        config.CreatedAt = DateTime.UtcNow.ToLocalTime();
        config.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        _dbContext.Set<McpServerConfig>().Add(config);
        await _dbContext.SaveChangesAsync(ct);
        return MapMcpServerConfigToDto(config);
    }

    public async Task<McpServerConfig?> UpdateMcpServerConfigAsync(McpServerConfig config, CancellationToken ct = default)
    {
        var existing = await _dbContext.Set<McpServerConfig>()
            .FirstOrDefaultAsync(c => c.Id == config.Id, ct);
        if (existing == null) return null;

        existing.IsEnabled = config.IsEnabled;
        existing.ConnectionString = config.ConnectionString;
        existing.Description = config.Description;
        existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        await _dbContext.SaveChangesAsync(ct);
        return MapMcpServerConfigToDto(existing);
    }

    public async Task<bool> DeleteMcpServerConfigAsync(int id, CancellationToken ct = default)
    {
        var entity = await _dbContext.Set<McpServerConfig>()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity == null) return false;

        _dbContext.Set<McpServerConfig>().Remove(entity);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public static McpServerConfig MapMcpServerConfigToDto(McpServerConfig c) => new()
    {
        Id = c.Id, IsEnabled = c.IsEnabled,
        ConnectionString = c.ConnectionString,
        Description = c.Description,
        CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
    };

    private async Task ClearDefaultForTypeAsync(string modelType, CancellationToken ct)
    {
        var defaults = await _dbContext.Set<LlmModelConfig>()
            .Where(m => m.ModelType == modelType && m.IsDefault)
            .ToListAsync(ct);
        foreach (var d in defaults)
        {
            d.IsDefault = false;
            d.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        }
    }
}
