using Microsoft.AspNetCore.Mvc;
using System.Text;
using Python.Runtime;

namespace ToolMng.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PythonController : ControllerBase
    {
        //private readonly ScriptEngine _engine;
        //private readonly dynamic _scope;
        

        static PythonController()
        {
            //_engine = Python.CreateEngine();
            //_scope = _engine.CreateScope();
            Runtime.PythonDLL = @"C:\\Program Files\\Python312\\python312.dll";            

        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecutePythonCode([FromBody] string code)
        {
            // 初始化 Python 运行时
            PythonEngine.Initialize();
            //Console.WriteLine("初始化成功");
            //PythonEngine.BeginAllowThreads();
            dynamic result;

            Console.WriteLine(PythonEngine.PythonPath);
            // 添加 Python 解释器的搜索路径，如果有特定的Python环境或库路径需要设置
            //PythonEngine.PythonPath = @"path\to\your\python\Lib\site-packages";
                // 使用 GIL (全局解释器锁)
            using (Py.GIL())
                {
                Console.WriteLine(1);
                // 动态创建Python Scope
                dynamic scope = Py.CreateScope();

                    // 在Scope中执行Python代码
                    result = scope.Exec(code);
                    Console.WriteLine(result);
                PythonEngine.BeginAllowThreads();
                // 如果需要获取输出，确保Python代码有明确的返回值或输出到stdout
                // 注意：直接获取print的输出较为复杂，通常建议Python代码显式返回结果
                //PythonEngine.Shutdown();
                // We needs to ensure that no any engines are running.
                

                // 尝试获取并返回结果，注意这里假设Python代码有返回值

            }
            if (PythonEngine.IsInitialized)
            {
                PythonEngine.Shutdown();
            }
            if (result != null)
            {
                return Ok(result.ToString());
            }
            else
            {
                return Ok("No explicit return value from Python script.");
            }
        }



    }
}
