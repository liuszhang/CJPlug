using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

namespace CJ.Plug.AIChat.Services;

public class IngestedChunk
{
    public const int VectorDimensions = 384; // 384 is the default vector size for the all-minilm embedding model
    public const string VectorDistanceFunction = DistanceFunction.CosineDistance;
    public const string CollectionName = "data-cj_plug_aichat-chunks";

    [VectorStoreKey(StorageName = "key")]
    [JsonPropertyName("key")]
    public required Guid Key { get; set; }

    [VectorStoreData(StorageName = "documentid")]
    [JsonPropertyName("documentid")]
    public required string DocumentId { get; set; }

    [VectorStoreData(StorageName = "content")]
    [JsonPropertyName("content")]
    public required string Text { get; set; }

    [VectorStoreData(StorageName = "context")]
    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction, StorageName = "embedding")]
    [JsonPropertyName("embedding")]
    public string? Vector => Text;
}
