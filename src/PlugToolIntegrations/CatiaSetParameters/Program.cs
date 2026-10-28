namespace CatiaSetParameters
{
    /// <summary>
    /// Catia 设置模型参数控制台（net4.8 + COM late-binding）。
    /// 自启 CATIA 会话，按 "key=value,key=value" 更新 Parameters，Update + Save 后输出结果到 stdout。
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
                {
                    Console.WriteLine("模型无更新");
                    return;
                }

                Console.WriteLine("Catia开始处理更新参数");
                string filePath = args[0];
                string exp = args[1];

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

                    // 解析 key=value 参数串（与 NX NewParameterString 同格式）
                    var keyValuePairs = new Dictionary<string, string>();
                    foreach (var pair in exp.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = pair.Split('=');
                        if (kv.Length >= 2)
                        {
                            keyValuePairs[kv[0].Trim()] = kv[1].Trim();
                        }
                    }

                    if (keyValuePairs.Count == 0)
                    {
                        Console.WriteLine("参数串格式无效，应为 key=value,key=value");
                        return;
                    }

                    // 按参数名更新
                    foreach (var kv in keyValuePairs)
                    {
                        dynamic param = part.Parameters.Item(kv.Key);
                        if (param == null)
                        {
                            Console.WriteLine($"未找到参数：{kv.Key}");
                            continue;
                        }
                        param.Value = kv.Value; // CATIA 按参数类型自动转换
                    }

                    part.Update();
                    Console.WriteLine("模型更新成功");

                    // CATPart 保存
                    doc.Save();
                    Console.WriteLine("模型保存成功");
                }
                finally
                {
                    try { app.Quit(); } catch { }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
