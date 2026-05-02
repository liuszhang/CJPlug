namespace ThreeJsIntegration.Models;

/// <summary>
/// 模型加载完成后的结果信息。
/// </summary>
public class ModelLoadResult
{
    /// <summary>模型包围盒尺寸</summary>
    public ModelSize Size { get; set; } = new();

    /// <summary>模型顶点数</summary>
    public int VertexCount { get; set; }
}

/// <summary>
/// 模型包围盒三维尺寸。
/// </summary>
public class ModelSize
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
