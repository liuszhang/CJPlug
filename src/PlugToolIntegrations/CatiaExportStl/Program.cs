namespace CatiaExportStl
{
    /// <summary>
    /// Catia 导出 STL 控制台（net4.8 + COM late-binding）。
    /// 自启 CATIA 会话，经 Shape.STLExport 设置弦高 Sag / RelativeSegments / Mode / OutputFormat（Binary 默认）后 Execute 导出。
    /// 注意：STLExport 属性名以图站实际 CATIA 类型库为准（方案 2.2 注），此处按 V5 R2x 常用命名。
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.WriteLine("无法单独使用，请配合流程数据传递使用");
                    return;
                }

                var filePath = args[0];
                var stlPath = args[1];
                // 弦高偏差 Sag（默认 0.1mm，可由变量覆盖）
                var sag = 0.1;
                if (args.Length >= 3 && double.TryParse(args[2], out var parsedSag))
                {
                    sag = parsedSag;
                }

                Console.WriteLine("Catia开始处理STL导出");

                var catiaType = Type.GetTypeFromProgID("CATIA.Application");
                if (catiaType == null)
                {
                    Console.WriteLine("未找到 CATIA.Application ProgID，请确认图站已安装 CATIA 并完成 COM 注册");
                    return;
                }
                dynamic app = Activator.CreateInstance(catiaType)!;
                app.Visible = false;

                try
                {
                    dynamic doc = app.Documents.Open(filePath);
                    dynamic part = GetPart(doc);
                    if (part == null)
                    {
                        Console.WriteLine("文件无模型数据！");
                        return;
                    }

                    // 创建 STL 导出对象并设置参数
                    dynamic stlExport = part.Shape.STLExport(stlPath);
                    stlExport.Sag = sag;                    // 弦高偏差
                    stlExport.RelativeSegments = false;     // 绝对弦高模式
                    stlExport.Mode = 1;                     // 1=实体（0=曲面）
                    stlExport.OutputFormat = 1;             // 1=Binary（0=ASCII，决策 #5 默认 Binary）
                    stlExport.Execute();

                    Console.WriteLine("STL导出完成");
                }
                finally
                {
                    try { app.Quit(); } catch { }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("导出过程出错：" + e);
            }
        }

        /// <summary>
        /// 根据文档类型取 Part / Product 对象
        /// </summary>
        static dynamic? GetPart(dynamic doc)
        {
            try
            {
                string docType = doc.GetType().Name;
                if (docType == "PartDocument")
                {
                    return doc.Part;
                }
                if (docType == "ProductDocument")
                {
                    return doc.Product;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
