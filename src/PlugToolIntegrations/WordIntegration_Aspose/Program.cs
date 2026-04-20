using Aspose.Words;
using Microsoft.VisualBasic;
using System.Reflection.Metadata;
using Document = Aspose.Words.Document;

namespace WordIntegration_Aspose
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            HookManager.ShowHookDetails(true);
            HookManager.StartHook();
            Document doc = new Document("C:\\tmp\\test0216.docx");
            // 将文档保存为 PDF 格式
            doc.Save("C:\\tmp\\test0216.pdf", SaveFormat.Pdf);
        }
    }
}
