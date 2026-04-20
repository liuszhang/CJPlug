using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpPlug.Services
{
    public class CodeRunner
    {
        public static async Task<string> RunCSharpCodeAsync(string code, string[] references)
        {
            // 引用必要的程序集
            var options = ScriptOptions.Default
                .WithReferences(references)  // 加载指定的 SDK（程序集路径）
                .WithImports("System", "System.Collections.Generic"); // 导入命名空间

            try
            {
                // 执行代码并返回结果
                var result = await CSharpScript.EvaluateAsync(code, options);
                return result?.ToString() ?? "执行成功，但无返回值";
            }
            catch (Exception ex)
            {
                return $"执行错误: {ex.Message}";
            }
        }
    }
}
