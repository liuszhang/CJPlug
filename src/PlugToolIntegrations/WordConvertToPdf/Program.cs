
using Microsoft.Office.Interop.Word;
using System.Diagnostics;
using System.Reflection.Metadata;
using Document = Microsoft.Office.Interop.Word.Document;

namespace WordConvertToPdf
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 替换为你的Word文件路径
            string wordFilePath = args[0];
            //string pdfFilePath = args[0];

            Console.WriteLine(wordFilePath);

            // LibreOffice 安装路径
            string libreOfficePath = @"C:\\Program Files\\LibreOffice\\program\\soffice.exe";
            //string libreOfficePath = @"D:\迅雷下载\LibreOfficePortable\App\libreoffice\program\soffice.exe";

            // 命令行参数
            string arguments = $"--headless --convert-to pdf --outdir {Path.GetDirectoryName(wordFilePath)} {wordFilePath}";
            Console.WriteLine(arguments);
            // 启动进程
            ProcessStartInfo processStartInfo = new ProcessStartInfo(libreOfficePath, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }

            
        }
    }
}
