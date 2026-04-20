using NXOpen;

namespace NXRunJournal
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {

                if (args.Length == 0)
                {
                    //Console.WriteLine("无法单独使用，请配合流程数据传递使用");
                    //Console.ReadKey();
                    //return;
                }

                Console.WriteLine("NX开始执行脚本");
                //Console.WriteLine("root:"+args[0]);
                //Console.WriteLine("root:" + args[1]);
                Session theSession = Session.GetSession();

                string scriptPath = @"C:\tmp\journal.vb";
                string[] outputLines = new string[0];
                Console.WriteLine(theSession.JournalManager.JournalLanguage);

                theSession.JournalManager.PlayDotNetJournal(scriptPath, outputLines);

                Console.WriteLine("处理完成");
            }
            catch (NXException ex)
            {
                Console.WriteLine($"NX异常: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"常规异常: {ex.Message}");
            }
        }
    }
}
