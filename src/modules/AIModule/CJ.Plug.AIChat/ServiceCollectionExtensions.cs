using CJ.Plug.AIChat.Services;
using CJ.Plug.AIChat.Services.Ingestion;
using CJ.Plug.Models.Abstractions;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.Extensions.AI;
using OllamaSharp;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIChatServices(this IServiceCollection services)
    {

        IChatClient chatClient = new OllamaApiClient(new Uri("http://localhost:11434"),
//"llama3.2");
"qwen3:4b");
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(new Uri("http://localhost:11434"),
            "all-minilm");

        var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
        var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
        services.AddSqliteVectorStore(_ => vectorStoreConnectionString);
        services.AddSqliteCollection<string, IngestedChunk>(IngestedChunk.CollectionName, vectorStoreConnectionString);

        services.AddSingleton<DataIngestor>();
        services.AddSingleton<SemanticSearch>();
        services.AddKeyedSingleton("ingestion_directory", new DirectoryInfo(Path.Combine(GlobalData.MainWebFileServer, "Data")));
        services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
        services.AddEmbeddingGenerator(embeddingGenerator);

        return services;
    }

}

