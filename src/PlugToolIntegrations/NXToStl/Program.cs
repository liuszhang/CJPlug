using NXOpen;

namespace NXToStl
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {

                if (args.Length == 0)
                {
                    Console.WriteLine("无法单独使用，请配合流程数据传递使用");
                    //Console.ReadKey();
                    //return;
                }

                Console.WriteLine("NX开始处理轻量化导出");
                //Console.WriteLine("root:"+args[0]);
                //Console.WriteLine("root:" + args[1]);
                Session theSession = Session.GetSession();
                // ----------------------------------------------
                //   菜单：文件(F)->打开(O)...
                // ----------------------------------------------
                BasePart basePart1;
                PartLoadStatus partLoadStatus1;
                //args[0] = "D:\\tmp\\123.prt";
                basePart1 = theSession.Parts.OpenActiveDisplay(args[0], DisplayPartOption.AllowAdditional, out partLoadStatus1);

                Part workPart = theSession.Parts.Work;
                Part displayPart = theSession.Parts.Display;
                partLoadStatus1.Dispose();
                theSession.ApplicationSwitchImmediate("UG_APP_MODELING");

                // ----------------------------------------------
                //   菜单：文件(F)->导出(E)->STL...
                // ----------------------------------------------


                STLCreator sTLCreator1;
                sTLCreator1 = theSession.DexManager.CreateStlCreator();

                sTLCreator1.AutoNormalGen = true;

                sTLCreator1.ChordalTol = 0.080000000000000002;

                sTLCreator1.AdjacencyTol = 0.080000000000000002;

                //sTLCreator1.OutputFile = "D:\\tmp\\456.stl";
                //args[1] = "D:\\tmp\\456.stl";

                sTLCreator1.OutputFile = args[1];

                workPart.Bodies.ToArray();
                foreach (var b in workPart.Bodies.ToArray())
                {
                    sTLCreator1.ExportSelectionBlock.Add(b);
                }

                NXObject nXObject1;
                nXObject1 = sTLCreator1.Commit();
                sTLCreator1.Destroy();

                Console.WriteLine("处理完成");
            }
            catch (Exception e)
            {
                Console.WriteLine("转换过程出错："+e);
            }
        }
    }
}
