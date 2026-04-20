using System;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            string wordFilePath = args[0];
            string pdfFilePath = args[1];
            //using (var converter = new Converter())
            //{
            //    converter.Convert(wordFilePath, pdfFilePath);
            //}

            Console.WriteLine("处理完成");
        }
        catch (Exception e)
        {
            Console.WriteLine("转换过程出错：" + e);
        }
    }
}