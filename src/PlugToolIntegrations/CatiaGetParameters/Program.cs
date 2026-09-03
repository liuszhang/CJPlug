using System.Collections;
using Newtonsoft.Json;

namespace CatiaGetParameters
{
    /// <summary>
    /// Catia 获取模型参数控制台（net4.8 + COM late-binding）。
    /// 自启 CATIA 会话（决策 6.4），打开模型，遍历 Parameters 输出 name=value JSON 到 stdout（图站捕获为 ResultString）。
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
                {
                    Console.WriteLine("无法单独使用，请配合流程数据传递使用");
                    return;
                }

                var filePath = args[0];
                Console.WriteLine("Catia开始处理获取参数");

                // 自启 CATIA 会话（COM late-binding，版本无感）
                var catiaType = Type.GetTypeFromProgID("CATIA.Application");
                if (catiaType == null)
                {
                    Console.WriteLine("未找到 CATIA.Application ProgID，请确认图站已安装 CATIA 并完成 COM 注册");
                    return;
                }
                dynamic app = Activator.CreateInstance(catiaType)!;
                app.Visible = false; // 后台运行

                try
                {
                    dynamic doc = app.Documents.Open(filePath);
                    // CATPart -> doc.Part；CATProduct -> doc.Product
                    dynamic part = GetPart(doc);
                    if (part == null)
                    {
                        Console.WriteLine("文件无模型信息！");
                        return;
                    }

                    var parameters = new List<KeyValuePair<string, string>>();
                    foreach (dynamic param in (IEnumerable)part.Parameters)
                    {
                        try
                        {
                            var name = (string)param.Name;
                            var value = Convert.ToString(param.Value);
                            parameters.Add(new KeyValuePair<string, string>(name, value ?? ""));
                        }
                        catch
                        {
                            // 单个参数读取失败不影响整体
                        }
                    }

                    string jsonResult = JsonConvert.SerializeObject(parameters);
                    Console.WriteLine(jsonResult);
                }
                finally
                {
                    try { app.Quit(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
