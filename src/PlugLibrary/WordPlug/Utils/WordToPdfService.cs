using Microsoft.Office.Interop.Word;

public class WordToPdfService
{
    public static void ConvertToPdf(string wordFilePath, string pdfFilePath)
    {
        Application wordApp = new Application();
        Document wordDoc = null;

        try
        {
            // 打开 Word 文档
            wordDoc = wordApp.Documents.Open(wordFilePath);

            // 保存为 PDF
            wordDoc.SaveAs2(pdfFilePath, WdSaveFormat.wdFormatPDF);

            // 关闭文档
            wordDoc.Close();
        }
        finally
        {
            // 退出 Word 应用程序
            wordApp.Quit();
        }
    }
}
