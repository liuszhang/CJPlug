using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CJ.Plug.Models.Job;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace PlugExecutionEngineRegressionTests;

/// <summary>
/// PlugExecutionEngine 内部类访问助手。
/// 由于 PlugExecutionEngine / PlugBookmarkManager / McpWorkflowRunner 标记为 internal，
/// 使用反射绕过 C# 可见性限制。
/// </summary>
internal static class EngineAccessor
{
    private const string AssemblyName = "CJ.Plug.ExecuteApi";

    public static object CreateEngine(
        MainDbContext dbContext,
        IPlugManageService plugManageService,
        IEnumerable<IPlugCommonExecute> plugCommonExecutes,
        IPlugExecuteHandlerService plugExecuteHandlerService,
        IPDZManageService pdzManageService,
        IPlugDataService plugDataService,
        object bookmarkManager,
        object mcpWorkflowRunner)
    {
        var engineType = GetInternalType(AssemblyName, "PlugExecutionEngine");

        // 通过遍历构造器签名找到匹配的（避免 hard-code 具体类型）
        var ctors = engineType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var ctor = ctors.FirstOrDefault()!;
        if (ctors.Length > 1)
        {
            // PlugExecutionEngine 只有 1 个 ctor，但如果多，取第一个非默认的
            ctor = ctors.FirstOrDefault(c => c.GetParameters().Length == 8) ?? ctor;
        }

        return ctor.Invoke(new object?[]
        {
            dbContext,
            plugManageService,
            plugCommonExecutes,
            plugExecuteHandlerService,
            pdzManageService,
            plugDataService,
            bookmarkManager,
            mcpWorkflowRunner,
        })!;
    }

    public static MethodInfo GetStartExecutePlugMethod()
    {
        var engineType = GetInternalType(AssemblyName, "PlugExecutionEngine");
        return engineType.GetMethod("StartExecutePlug", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public static MethodInfo GetReportExecuteResultMethod()
    {
        var engineType = GetInternalType(AssemblyName, "PlugExecutionEngine");
        return engineType.GetMethod("ReportExecuteResult", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    public static Type GetInternalType(string assemblyName, string typeName)
    {
        // 类型在默认命名空间下，无 namespace 修饰
        return Type.GetType($"{typeName}, {assemblyName}", throwOnError: true)!;
    }
}

/// <summary>
/// 内部测试辅助：构造 PlugExecutionEngine 实例并暴露测试方法。
/// </summary>
internal class EngineUnderTest
{
    private readonly object _engine;

    public EngineUnderTest(
        IPlugManageService plugManageService,
        IPlugExecuteHandlerService plugExecuteHandlerService,
        IPlugDataService plugDataService,
        IPDZManageService pdzManageService)
    {
        // 构造一个真实的 MainDbContext 实例（不实际查询数据库，仅满足 ctor）。
        // PlugExecutionEngine 中对 _dbContext 的使用都是 .Set<T>().FirstOrDefaultAsync(...)
        // 我们没有用到的 DB 查询路径会被 NRE（因为 DbSet 未实际映射），但 StartExecutePlug 的
        // 6 个守卫点都不需要 DB 查询，因此测试可以正常走完。
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbContext = new MainDbContext(options);
        var plugCommonExecutes = Array.Empty<IPlugCommonExecute>();

        // internal 类（PlugBookmarkManager / McpWorkflowRunner）通过 FormatterServices
        // 绕过 ctor 创建未初始化实例。它们在 StartExecutePlug 的 6 个守卫路径中不会被调用。
        var bookmarkManagerType = EngineAccessor.GetInternalType("CJ.Plug.ExecuteApi", "PlugBookmarkManager");
        var bookmarkManager = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(bookmarkManagerType);

        var mcpRunnerType = EngineAccessor.GetInternalType("CJ.Plug.ExecuteApi", "McpWorkflowRunner");
        var mcpWorkflowRunner = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(mcpRunnerType);

        _engine = EngineAccessor.CreateEngine(
            dbContext,
            plugManageService,
            plugCommonExecutes,
            plugExecuteHandlerService,
            pdzManageService,
            plugDataService,
            bookmarkManager,
            mcpWorkflowRunner);
    }

    public async Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request)
    {
        var method = EngineAccessor.GetStartExecutePlugMethod();
        var task = (Task)method.Invoke(_engine, new object?[] { request })!;
        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result");
        return (ExecuteResultData?)resultProp?.GetValue(task);
    }

    public async Task ReportExecuteResult(ExecuteResultData erd)
    {
        var method = EngineAccessor.GetReportExecuteResultMethod();
        var task = (Task)method.Invoke(_engine, new object?[] { erd })!;
        await task.ConfigureAwait(false);
    }
}
