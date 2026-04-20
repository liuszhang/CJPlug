class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            args = ["a", "b", "c", "--a", "666", "--b", "888", "--c", "999"];
        }
        //args = ["a", "b", "c","--a","666","--b","888","--c","999"];
        // 检查是否有参数传递进来
        if (args.Length == 0)
        {
            Console.WriteLine("请至少提供一个参数。");
            Console.ReadLine();
            return;
        }
        Console.WriteLine(String.Join(",", args));
        Console.WriteLine("你输入的参数有：");
        foreach (var arg in args)
        {
            Console.WriteLine($"- {arg}");

        }
        for (int i = 0; i < (args.Length/3); i++) 
        {
            var optionValue = "";
            // 遍历并打印所有参数
            foreach (var arg in args)
            {
                //Console.WriteLine($"- {arg}");
                // 示例：查找名为 "--option" 的参数，并打印其后的值
                optionValue = args.SkipWhile(a => !a.Equals("--" + args[i], StringComparison.OrdinalIgnoreCase))
                                    .Skip(1)
                                    .FirstOrDefault();
                
            }
            Console.WriteLine($"--{args[i]}的值为: {optionValue}");
        }
        

        // 示例：查找名为 "--option" 的参数，并打印其后的值
        //var optionValue = args.SkipWhile(a => !a.Equals("--option", StringComparison.OrdinalIgnoreCase))
        //                    .Skip(1)
        //                    .FirstOrDefault();

        //if (optionValue != null)
        //{
        //    Console.WriteLine($"--option 的值为: {optionValue}");
        //}
        //else
        //{
        //    Console.WriteLine("--option 参数未找到。");
        //}

        Console.ReadLine();
    }
}