using Xceed.Words.NET;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("无参数!");
            return;
        }
        string fileName = args[0];
        //Console.WriteLine(fileName);
        fileName =Path.GetFullPath(fileName);
        Console.WriteLine(fileName);
        string bookmark=args[1];
        Console.WriteLine(bookmark);
        string textInsert=args[2];
        Console.WriteLine(textInsert);

        try
        {
            Console.WriteLine($"加载文件：{fileName}");
            //InsertTextAtBookmark(@"C:\tmp\test.docx", "bmtest", "xx");
            using (var document = DocX.Load(fileName))
            {

                Console.WriteLine("加载成功!");
                // Insert new text before a document’s bookmark.
                document.InsertAtBookmark(textInsert, bookmark);

                document.Save();

                Console.WriteLine("更新文件成功：" + fileName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"写入失败:{ex.Message}");
            return;
        }


        
    }

}

