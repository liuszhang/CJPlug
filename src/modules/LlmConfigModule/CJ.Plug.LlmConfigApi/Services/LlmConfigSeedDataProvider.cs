using CJ.Plug.LlmConfigModel.Models;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.LlmConfigApi.Services;

public class LlmConfigSeedDataProvider : ISeedDataProvider
{
    public string Name => "LLM 配置模块种子数据";
    public int Order => 125;

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dbContext = serviceProvider.GetRequiredService<MainDbContext>();

        var existing = await dbContext.Set<LlmProvider>().FirstOrDefaultAsync(cancellationToken);
        if (existing != null)
        {
            Console.WriteLine("[SeedData] LlmConfig 种子数据已存在，跳过");
            return;
        }

        var now = DateTime.UtcNow.ToLocalTime();

        // ---- Ollama (本地) ----
        var ollama = new LlmProvider
        {
            Name = "ollama", DisplayName = "Ollama (本地)",
            ApiBaseUrl = "http://localhost:11434/v1",
            Description = "本地 Ollama 服务",
            SortOrder = 1, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Set<LlmProvider>().Add(ollama);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Set<LlmModelConfig>().AddRange(
            new LlmModelConfig { LlmProviderId = ollama.Id, ModelName = "qwen3:4b",    DisplayName = "Qwen3 4B",      ModelType = "Chat", MaxTokens = 4096, Temperature = 0.7, IsDefault = true },
            new LlmModelConfig { LlmProviderId = ollama.Id, ModelName = "qwen3:1.7b",  DisplayName = "Qwen3 1.7B",    ModelType = "Chat", MaxTokens = 2048, Temperature = 0.7, IsDefault = false },
            new LlmModelConfig { LlmProviderId = ollama.Id, ModelName = "deepseek-r1:1.5b", DisplayName = "DeepSeek R1 1.5B", ModelType = "Chat", MaxTokens = 2048, Temperature = 0.7, IsDefault = false }
        );
        await dbContext.SaveChangesAsync(cancellationToken);
        Log.Information("[SeedData] LLM 种子数据: Ollama 供应商及 3 个模型已创建");

        // ---- OpenRouter ----
        var openRouter = new LlmProvider
        {
            Name = "openrouter", DisplayName = "OpenRouter",
            ApiBaseUrl = "https://openrouter.ai/api/v1",
            Description = "OpenRouter AI 网关（支持多种模型）",
            SortOrder = 2, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Set<LlmProvider>().Add(openRouter);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Set<LlmModelConfig>().AddRange(
            new LlmModelConfig { LlmProviderId = openRouter.Id, ModelName = "openrouter/free", DisplayName = "OpenRouter Free", ModelType = "Chat", MaxTokens = 4096, Temperature = 0.7, IsDefault = false }
        );
        await dbContext.SaveChangesAsync(cancellationToken);
        Log.Information("[SeedData] LLM 种子数据: OpenRouter 供应商及 1 个模型已创建");

        // ---- DeepSeek ----
        var deepseek = new LlmProvider
        {
            Name = "deepseek", DisplayName = "DeepSeek",
            ApiBaseUrl = "https://api.deepseek.com",
            Description = "DeepSeek API",
            SortOrder = 3, CreatedAt = now, UpdatedAt = now
        };
        dbContext.Set<LlmProvider>().Add(deepseek);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Set<LlmModelConfig>().AddRange(
            new LlmModelConfig { LlmProviderId = deepseek.Id, ModelName = "deepseek-v4-pro", DisplayName = "DeepSeek V4 Pro", ModelType = "Chat", MaxTokens = 4096, Temperature = 0.7, IsDefault = false }
        );
        await dbContext.SaveChangesAsync(cancellationToken);
        Log.Information("[SeedData] LLM 种子数据: DeepSeek 供应商及 1 个模型已创建");

        Console.WriteLine("[SeedData] LLM 配置模块种子数据创建完成");
    }
}
