using Newtonsoft.Json;
using NXOpen;
using NXOpen.CAE;
using NXOpen.UF;

namespace NXGetParameters
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
                    return;
                }
                //Console.WriteLine("NX开始处理获取参数");
                //Console.WriteLine("Hello, World!");
                var filePath = args[0];
                // 初始化NX Session
                //Console.WriteLine("NX开始处理");
                //Console.WriteLine("root:"+args[0]);
                //Console.WriteLine("root:" + args[1]);
                NXOpen.Session session = Session.GetSession();

                // 打开模型文档
                BasePart workPart = session.Parts.Work;
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
                        //Console.WriteLine(paramName + "=" + paramValue);
                        //Console.WriteLine(paramName);
                        //Console.WriteLine(parameter.GetListValue().ToString());
                        //Console.WriteLine(parameter.ToString());
                        //Console.WriteLine(parameter.Equation);
                        //Console.WriteLine(parameter.Description);
                        //Console.WriteLine(parameter.Name);
                        //Console.WriteLine(parameter.Value);
                        output += (parameter.Equation) + " ";
                    }
                    // 将数据序列化成JSON并写入标准输出
                    string jsonResult = JsonConvert.SerializeObject(output);
                    Console.WriteLine(jsonResult);
                }
                else
                {
                    Console.WriteLine("文件无模型信息！");
                }
                // 关闭NX Session
                session.CloseTestOutput();



                // 设置退出状态码为0表示成功
                //Environment.Exit(0);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
            

        }
    }
}
