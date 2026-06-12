using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using FluentAssertions;
using NSubstitute;
using PlugExecutionEngineRegressionTests;
using Xunit;

namespace PlugExecutionEngineRegressionTests;

/// <summary>
/// MCP Plugin 路径不回归检查。
/// 工程师已说明第 234-298 行的 MCP Plugin 路径中 handler?.PlugCommonExecute(...) 由于前置
/// plug==null 拦截（line 153-162），不会出现 NRE。
/// 本测试套件验证：
///   J1.  MCP Plugin 路径 + plug==null：报"未找到插头"错误，handler?. 不会引发 NRE
///   J2.  MCP Plugin 路径 + plug!=null + handler==null：当前实现下 handler?. 会返回 null（不抛 NRE）
///        — 验证 NRE 不会爆发；这是工程师明确不修的路径
/// </summary>
public class McpPathRegressionTests
{
    private const string TestPlugDefinitionId = "mcp-plug-001";
    private const string TestPlugName = "MCP测试插头";
    private const string TestPlugTypeKey = "CSharpPlug";

    private readonly IPlugManageService _plugManageService = Substitute.For<IPlugManageService>();
    private readonly IPlugExecuteHandlerService _handlerService = Substitute.For<IPlugExecuteHandlerService>();
    private readonly IPlugDataService _plugDataService = Substitute.For<IPlugDataService>();
    private readonly IPDZManageService _pdzService = Substitute.For<IPDZManageService>();
    private readonly EngineUnderTest _engine;

    public McpPathRegressionTests()
    {
        _engine = new EngineUnderTest(_plugManageService, _handlerService, _plugDataService, _pdzService);
    }

    /// <summary>
    /// J1. MCP Plugin 路径 + plug==null：前置拦截起作用，不抛 NRE。
    /// </summary>
    [Fact]
    public async Task J1_MCP路径_Plug不存在_应前置拦截_不抛NRE()
    {
        // Arrange
        var request = BuildMcpPluginRequest();
        // 关键：plug 找不到，触发 153-162 行的前置拦截
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns((Plug?)null);

        // Act
        var act = async () => await _engine.StartExecutePlug(request);

        // Assert
        await act.Should().NotThrowAsync();
        var result = await _engine.StartExecutePlug(request);
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ExecuteStatus.Should().Be(JobStatus.完成);
        erd.ExecuteSubStatus.Should().Be(JobSubStatus.出错);
    }

    /// <summary>
    /// J2. MCP Plugin 路径 + plug!=null + handler==null：
    /// 当前代码 (line 285 handler?.PlugCommonExecute(...)) 静默返回 null。
    /// 验证：不会抛 NRE（因为 handler?. 是空条件运算符）。
    /// 工程师已知这是潜在问题但本次不修。
    /// </summary>
    [Fact]
    public async Task J2_MCP路径_Handler为null_不抛NRE_静默返回null()
    {
        // Arrange
        var request = BuildMcpPluginRequest();
        var plug = BuildPlug();
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns(plug);
        _handlerService.GetExecuteHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);
        _handlerService.GetCategoryFallbackHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);

        // Act + Assert
        var act = async () => await _engine.StartExecutePlug(request);
        // 验证不抛 NRE
        await act.Should().NotThrowAsync();
    }

    private static PlugExecutionRequest BuildMcpPluginRequest()
    {
        return new PlugExecutionRequest
        {
            PlugTypeKey = TestPlugTypeKey,
            McpToolType = "Plugin",
            ExecuteMode = ExecuteMode.Standalone,
            ExecuteResultData = new ExecuteResultData
            {
                Ids = new ExecuteIdsBundle
                {
                    PlugDefinitionId = TestPlugDefinitionId,
                    PDZId = null,
                    ExecuteTaskPlugIds = new List<string>() // Count==0 走首次执行
                }
            }
        };
    }

    private static Plug BuildPlug() => new()
    {
        Id = 1,
        DefinitionId = TestPlugDefinitionId,
        Name = TestPlugName,
        PlugTypeKey = TestPlugTypeKey,
        Category = "桌面类_自研",
    };
}
