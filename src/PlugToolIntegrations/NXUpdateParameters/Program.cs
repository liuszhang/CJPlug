using Newtonsoft.Json;
using NXOpen;
using NXOpen.CAE;
using System.Diagnostics;
using static NXOpen.CAE.Post;

namespace NXUpdateParameters
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Console.WriteLine("Hello, World!");
                if (args.Length < 2)
                {
                    Console.WriteLine("模型无更新");
                    //Console.ReadKey();
                    return;
                }
                Console.WriteLine("NX开始处理更新参数");
                string filePath = args[0];
                string exp = args[1];
                //string filePath = @"D:\\tmp\\123.prt";
                //string exp = "p7=333,p8=120,p9=100,p10=50,p11=0,p12=25,p13=45,p14=100";
                //Console.WriteLine(filePath);
                //Console.WriteLine(exp);

                NXOpen.Session session = Session.GetSession();

                // 打开模型文档
                BasePart workPart;
                NXOpen.PartLoadStatus partLoadStatus1;
                workPart = session.Parts.OpenActiveDisplay(filePath, NXOpen.DisplayPartOption.AllowAdditional, out partLoadStatus1);

                if (workPart != null)
                {
                    //var attr = workPart.GetUserAttributes();
                    var attr = workPart.Expressions;
                    string output = "";
                    // 遍历模型参数
                    foreach (var parameter in attr.ToArray())
                    {
                        //string paramName = parameter.GetListValue();
                        //object paramValue = parameter.RealValue;
                        // 处理参数名称和值
                        output += (parameter.Equation) + " ";
                    }
                    // 将数据序列化成JSON并写入标准输出
                    //string jsonResult = JsonConvert.SerializeObject(output);
                    Console.WriteLine("原始参数：");
                    Console.WriteLine(output);
                    output = "";

                    Dictionary<string, double> keyValuePairs = exp.Split(',')
                        .Select(pair => pair.Split('='))
                        .ToDictionary(split => split[0], split => double.Parse(split[1]));

                    NXOpen.Expression expression1;
                    NXOpen.Unit unit1 = (NXOpen.Unit)workPart.UnitCollection.FindObject("MilliMeter");
                    NXOpen.NXObject[] objects1 = new NXOpen.NXObject[keyValuePairs.Count];
                    int i = 0;
                    foreach (var keyValue in keyValuePairs)
                    {
                        //Console.WriteLine(keyValue.ToString());
                        expression1 = attr.FindObject(keyValue.Key);
                        attr.EditWithUnits(expression1, unit1, keyValue.Value.ToString());
                        objects1[i] = expression1;
                        i++;
                    }

                    // 遍历模型参数
                    foreach (var parameter in attr.ToArray())
                    {
                        //string paramName = parameter.GetListValue();
                        //object paramValue = parameter.RealValue;
                        // 处理参数名称和值
                        output += (parameter.Equation) + " ";
                    }
                    // 将数据序列化成JSON并写入标准输出
                    //jsonResult = JsonConvert.SerializeObject(output);
                    Console.WriteLine("更新参数：");
                    Console.WriteLine(output);


                    NXOpen.Session.UndoMarkId markId5;
                    markId5 = session.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "Make Up to Date");
                    session.UpdateManager.MakeUpToDate(objects1, markId5);

                    NXOpen.Session.UndoMarkId markId6;
                    markId6 = session.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "NX update");
                    int nErrs1;
                    nErrs1 = session.UpdateManager.DoUpdate(markId6);
                    Console.WriteLine("模型更新成功");
                    session.DeleteUndoMark(markId6, "NX update");
                    session.DeleteUndoMark(markId5, null);
                    NXOpen.PartSaveStatus partSaveStatus1;
                    partSaveStatus1 = workPart.Save(NXOpen.BasePart.SaveComponents.True, NXOpen.BasePart.CloseAfterSave.False);

                    partSaveStatus1.Dispose();
                    Console.WriteLine("模型保存成功");
                }
                else
                {
                    Console.WriteLine("文件无模型数据！");
                }
                // 关闭NX Session
                session.CloseTestOutput();

                //Console.ReadKey();
            }
            catch (Exception e) 
            { 
                Console.WriteLine(e);
            }

            
        }
    }
}
