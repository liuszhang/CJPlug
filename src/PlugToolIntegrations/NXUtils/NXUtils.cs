//using Grandgain.NXOpen.Api;
using NXOpen;
using System.Runtime.CompilerServices;
using System.Windows.Threading;



namespace NXUtils
{
    public static class NXUtils
    {
        public static void OpenFile()
        {
            // 启动NX会话
            Console.WriteLine("调用NX");
            
            NXOpen.Session theSession = Session.GetSession();
            // ----------------------------------------------
            //   菜单：文件(F)->打开(O)...
            // ----------------------------------------------
            NXOpen.BasePart basePart1;
            NXOpen.PartLoadStatus partLoadStatus1;
            basePart1 = theSession.Parts.OpenActiveDisplay("D:\\tmp\\123.prt", NXOpen.DisplayPartOption.AllowAdditional, out partLoadStatus1);

            NXOpen.Part workPart = theSession.Parts.Work;
            NXOpen.Part displayPart = theSession.Parts.Display;
            partLoadStatus1.Dispose();
            theSession.ApplicationSwitchImmediate("UG_APP_MODELING");

            // ----------------------------------------------
            //   菜单：文件(F)->导出(E)->STL...
            // ----------------------------------------------
            NXOpen.Session.UndoMarkId markId1;
            markId1 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "开始");

            NXOpen.STLCreator sTLCreator1;
            sTLCreator1 = theSession.DexManager.CreateStlCreator();

            sTLCreator1.AutoNormalGen = true;

            sTLCreator1.ChordalTol = 0.080000000000000002;

            sTLCreator1.AdjacencyTol = 0.080000000000000002;

            sTLCreator1.OutputFile = "D:\\tmp\\456.stl";

            sTLCreator1.OutputFile = "D:\\tmp\\123.stl";

            theSession.SetUndoMarkName(markId1, "STL 导出 对话框");

            NXOpen.Body body1 = (NXOpen.Body)workPart.Bodies.FindObject("BLOCK(1)");
            bool added1;
            added1 = sTLCreator1.ExportSelectionBlock.Add(body1);

            NXOpen.Session.UndoMarkId markId2;
            markId2 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "STL 导出");

            theSession.DeleteUndoMark(markId2, null);

            NXOpen.Session.UndoMarkId markId3;
            markId3 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "STL 导出");

            NXOpen.NXObject nXObject1;
            nXObject1 = sTLCreator1.Commit();

            theSession.DeleteUndoMark(markId3, null);

            theSession.SetUndoMarkName(markId1, "STL 导出");

            sTLCreator1.Destroy();

            Console.WriteLine("处理完成");
            // ----------------------------------------------
            //   菜单：工具(T)->操作记录(J)->停止录制(S)
            // ----------------------------------------------

        }
        public static int GetUnloadOption(string dummy) { return (int)NXOpen.Session.LibraryUnloadOption.Immediately; }
    }
}
