using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq.Expressions;

namespace CJ.Plug_Aspire.StationApiService.Services
{
    public class ExecDllTest
    {
        // 导入 Win32 API 函数
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public void Invoke(string dllName,string methodName)
        {
            // 动态调用 DivideAndRemainder 函数
            var result = DynamicInvoke("example.dll", "DivideAndRemainder", new object[] { 10, 3 }, new Type[] { typeof(int), typeof(int) });
            Console.WriteLine($"Quotient: {result}, Remainder: {result}");
        }


        static object DynamicInvoke(string dllName, string methodName, object[] parameters, Type[] returnTypes)
        {
            // 加载 DLL
            IntPtr hModule = LoadLibrary(dllName);
            if (hModule == IntPtr.Zero)
            {
                throw new Exception("Failed to load DLL");
            }

            // 获取函数指针
            IntPtr pAddressOfFunctionToCall = GetProcAddress(hModule, methodName);
            if (pAddressOfFunctionToCall == IntPtr.Zero)
            {
                FreeLibrary(hModule);
                throw new Exception("Failed to get function address");
            }

            // 获取参数类型
            Type[] parameterTypes = parameters.Select(p => p.GetType()).ToArray();

            // 动态生成委托
            Delegate delegateInstance = CreateDelegate(pAddressOfFunctionToCall, parameterTypes, returnTypes);

            // 调用委托
            object result = delegateInstance.DynamicInvoke(new object[] { pAddressOfFunctionToCall }.Concat(parameters).ToArray());

            // 释放 DLL
            FreeLibrary(hModule);

            return result;
        }

        static Delegate CreateDelegate(IntPtr functionPointer, Type[] parameterTypes, Type[] returnTypes)
        {
            // 创建动态方法
            DynamicMethod dynamicMethod = new DynamicMethod(
                "DynamicInvokeMethod",
                typeof(Tuple<object[], IntPtr>),
                new Type[] { typeof(IntPtr) }.Concat(parameterTypes).ToArray(),
                typeof(Program).Module
            );

            // 创建IL生成器
            ILGenerator il = dynamicMethod.GetILGenerator();

            // 声明局部变量
            LocalBuilder resultArrayLocal = il.DeclareLocal(typeof(object[]));
            LocalBuilder functionPointerLocal = il.DeclareLocal(typeof(IntPtr));

            // 初始化结果数组
            il.Emit(OpCodes.Ldc_I4, returnTypes.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, resultArrayLocal);

            // 加载函数指针
            il.Emit(OpCodes.Ldarg_0); // 加载第一个参数（函数指针）
            il.Emit(OpCodes.Stloc, functionPointerLocal);

            // 加载其他参数
            for (int i = 1; i <= parameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i); // 加载第i个参数
            }

            // 调用非托管函数
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), parameterTypes, null);

            // 处理返回值
            for (int i = 0; i < returnTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, resultArrayLocal);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldind_Ref); // 加载指针指向的值
                il.Emit(OpCodes.Box, returnTypes[i]); // 将值装箱
                il.Emit(OpCodes.Stelem_Ref); // 存储到数组中
            }

            // 创建 Tuple
            il.Emit(OpCodes.Ldloc, resultArrayLocal);
            il.Emit(OpCodes.Ldloc, functionPointerLocal);
            il.Emit(OpCodes.Newobj, typeof(Tuple<object[], IntPtr>).GetConstructor(new Type[] { typeof(object[]), typeof(IntPtr) }));

            // 返回结果
            il.Emit(OpCodes.Ret);

            // 创建委托类型
            Type delegateType = Expression.GetDelegateType(new Type[] { typeof(IntPtr) }.Concat(parameterTypes).ToArray().Concat(new Type[] { typeof(Tuple<object[], IntPtr>) }).ToArray());

            // 创建委托
            return dynamicMethod.CreateDelegate(delegateType);
        }
    }
}
