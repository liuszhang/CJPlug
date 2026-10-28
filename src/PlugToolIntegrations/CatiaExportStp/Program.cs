namespace CatiaExportStp
{
    /// <summary>
    /// Catia 导出 STP 控制台（net4.8 + COM late-binding）。
    /// 自启 CATIA 会话，打开模型，Document.ExportData(path, "stp") 导出后输出结果到 stdout。
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
                var stpPath = args[1];
                Console.WriteLine("Catia开始处理STP导出");

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
                    doc.ExportData(stpPath, "stp");
                    Console.WriteLine("STP导出完成");
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
    }
}
