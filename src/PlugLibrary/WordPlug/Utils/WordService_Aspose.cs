using Aspose.Words;
using CJ.Plug.Models.Services;
using Microsoft.Office.Interop.Word;
using Bookmark = Aspose.Words.Bookmark;
using Document = Aspose.Words.Document;
using Task = System.Threading.Tasks.Task;

public class WordService_Aspose
{
    public static void ConvertToPdf(string wordFilePath, string pdfFilePath)
    {
        wordFilePath = wordFilePath.Trim('\"');
        pdfFilePath = pdfFilePath.Trim('\"');
        Console.WriteLine(wordFilePath);
        Console.WriteLine(pdfFilePath);
        HookManager.ShowHookDetails(false);
        HookManager.StartHook();
        
        try
        {
            // 加载 Word 文档
            Document doc = new Document(wordFilePath);
            Console.WriteLine("start convert word to pdf");
            // 保存为 PDF
            doc.Save(pdfFilePath, SaveFormat.Pdf);
            Console.WriteLine("convert word to pdf success");
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        
        //HookManager.StopHook();
        return;
    }

    public static void InsertHtmlAtBookmark(string filePath, string bookmarkName, string text)
    {
        HookAspose();
        // 加载文档
        Document doc = new Document(filePath);
        Bookmark bookmark = doc.Range.Bookmarks[bookmarkName];
        if (bookmark != null)
        {
            DocumentBuilder builder = new DocumentBuilder(doc);
            builder.MoveToBookmark(bookmarkName);
            builder.InsertHtml(text);
            doc.Save(filePath);
        }
        else
        {
            Console.WriteLine($"Bookmark '{bookmarkName}' not found in the document.");
        }
    }


    public static void InsertTextAtBookmark(string filePath, string bookmarkName, string text)
    {
        HookAspose();
        // 加载文档
        Console.WriteLine($"Inserting text at bookmark '{bookmarkName}' in document '{filePath}'");
        Document doc = new Document(filePath);
        Bookmark bookmark = doc.Range.Bookmarks[bookmarkName];
        if (bookmark != null)
        {
            DocumentBuilder builder = new DocumentBuilder(doc);
            builder.MoveToBookmark(bookmarkName);
            builder.Write(text);
            doc.Save(filePath);
        }
        else
        {
            Console.WriteLine($"Bookmark '{bookmarkName}' not found in the document.");
        }
    }

    public static void InsertTableAtBookmark(string filePath, string bookmarkName)
    {
        HookAspose();
        // 加载文档
        Document doc = new Document(filePath);
        Bookmark bookmark = doc.Range.Bookmarks[bookmarkName];
        if (bookmark != null)
        {
            DocumentBuilder builder = new DocumentBuilder(doc);
            builder.MoveToBookmark(bookmarkName);

            Table table = (Table)builder.StartTable();

            // 添加表头
            builder.InsertCell();
            builder.Write("姓名");
            builder.InsertCell();
            builder.Write("年龄");
            builder.EndRow();

            // 添加数据行
            builder.InsertCell();
            builder.Write("张三");
            builder.InsertCell();
            builder.Write("30");
            builder.EndRow();

            builder.EndTable();
            doc.Save(filePath);
        }
    }

    public static void InsertImageAtBookmark(string filePath, string bookmarkName, string imagePath)
    {
        HookAspose();
        // 加载文档
        Document doc = new Document(filePath);
        Bookmark bookmark = doc.Range.Bookmarks[bookmarkName];
        if (bookmark != null)
        {
            DocumentBuilder builder = new DocumentBuilder(doc);
            builder.MoveToBookmark(bookmarkName);
            builder.InsertImage(imagePath);
            doc.Save(filePath);
        }
    }

    static void HookAspose()
    {
        HookManager.ShowHookDetails(false);
        HookManager.StartHook();
    }
}
