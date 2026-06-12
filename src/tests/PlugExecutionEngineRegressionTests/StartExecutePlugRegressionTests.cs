using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApi.Contracts.PDZDatas;
using FluentAssertions;
using NSubstitute;
using PlugExecutionEngineRegressionTests;
using Xunit;

namespace PlugExecutionEngineRegressionTests;

/// <summary>
/// PlugExecutionEngine.StartExecutePlug 的 NRE BugFix 回归测试。
/// 对应 6 个守卫点：
///   守卫 1-2: 无插头ID + Standalone / 非 Standalone handler==null
///   守卫 3:   流程图来源 plug2==null
///   守卫 4:   流程图来源 handler==null
///   守卫 5:   动作执行 plug==null
///   守卫 6:   动作执行 handler==null
/// </summary>
public class StartExecutePlugRegressionTests
{
    private const string TestPlugDefinitionId = "6c3d52af013a672e";
    private const string TestPlugName = "测试桌面类插头";
    private const string TestPlugTypeKey = "CSharpPlug";

    private readonly IPlugManageService _plugManageService = Substitute.For<IPlugManageService>();
    private readonly IPlugExecuteHandlerService _handlerService = Substitute.For<IPlugExecuteHandlerService>();
    private readonly IPlugDataService _plugDataService = Substitute.For<IPlugDataService>();
    private readonly IPDZManageService _pdzService = Substitute.For<IPDZManageService>();
    private readonly EngineUnderTest _engine;

    public StartExecutePlugRegressionTests()
    {
        _engine = new EngineUnderTest(_plugManageService, _handlerService, _plugDataService, _pdzService);
    }

    #region A. 流程图来源 + plug2==null

    /// <summary>
    /// 场景 A: 流程图来源（ExecuteTaskPlugIds.Count==0 第一次执行），PlugData 有但 Plug 定义被删除（plug2==null）。
    /// 修复前：会进入 handler?.PlugCommonExecute，plug2 为 null 报 NRE。
    /// 修复后：调用 BuildAndReportErrorAsync 返回错误结果，ReportExecuteResult 被调用。
    /// </summary>
    [Fact]
    public async Task A_流程图来源_Plug定义缺失_应返回错误且不抛NRE()
    {
        // Arrange
        var request = BuildFirstTimeRequest();
        var plugData = BuildPlugData();
        _plugDataService.GetByPlugDefinitionIdAsync(Arg.Any<string>()).Returns(plugData);
        // 关键：plug2 == null
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns((Plug?)null);
        _pdzService.GetByPDZId(Arg.Any<string>()).Returns((PlugDataZone?)null);

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

    #endregion

    #region B. 流程图来源 + plug2!=null + handler==null

    /// <summary>
    /// 场景 B: 流程图来源，plug2 存在但 handler 找不到。
    /// 修复前：handler?.PlugCommonExecute 中 handler 为 null 报 NRE。
    /// 修复后：守卫 4 拦截，BuildAndReportErrorAsync 返回错误结果。
    /// </summary>
    [Fact]
    public async Task B_流程图来源_Handler缺失_应返回错误且不抛NRE()
    {
        // Arrange
        var request = BuildFirstTimeRequest();
        var plugData = BuildPlugData();
        var plug = BuildPlug();
        _plugDataService.GetByPlugDefinitionIdAsync(Arg.Any<string>()).Returns(plugData);
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns(plug);
        _pdzService.GetByPDZId(Arg.Any<string>()).Returns((PlugDataZone?)null);
        // 关键：handler 全找不到
        _handlerService.GetExecuteHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);
        _handlerService.GetCategoryFallbackHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ExecuteStatus.Should().Be(JobStatus.完成);
        erd.ExecuteSubStatus.Should().Be(JobSubStatus.出错);
    }

    #endregion

    #region C. 流程图来源 + plug2!=null + handler!=null (正常路径)

    /// <summary>
    /// 场景 C: 流程图来源正常路径：handler.PlugCommonExecute 被调用一次。
    /// 修复后：handler?. 改为 handler.（前置 NRE-check 已保证非空），正常调用 handler。
    /// </summary>
    [Fact]
    public async Task C_流程图来源_正常路径_应调用Handler_PlugCommonExecute_一次()
    {
        // Arrange
        var request = BuildFirstTimeRequest();
        var plugData = BuildPlugData();
        var plug = BuildPlug();
        var handler = Substitute.For<IPlugCommonExecute>();
        var expectedResult = new ExecuteResultData
        {
            ExecuteStatus = JobStatus.完成,
            ExecuteSubStatus = JobSubStatus.已完成,
            ResultString = "C-success"
        };
        handler.PlugCommonExecute(Arg.Any<ExecuteServiceContext>()).Returns(Task.FromResult<ExecuteResultData?>(expectedResult));

        _plugDataService.GetByPlugDefinitionIdAsync(Arg.Any<string>()).Returns(plugData);
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns(plug);
        _pdzService.GetByPDZId(Arg.Any<string>()).Returns((PlugDataZone?)null);
        _handlerService.GetExecuteHandler(Arg.Any<string>()).Returns(handler);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ResultString.Should().Be("C-success");
        await handler.Received(1).PlugCommonExecute(Arg.Any<ExecuteServiceContext>());
    }

    #endregion

    #region D. 动作执行 + plug==null

    /// <summary>
    /// 场景 D: 动作执行分支，ExecuteTaskPlugIds 有任务，PlugData 有但 Plug 定义缺失。
    /// 修复前：plug 为 null 注入到 ExecuteServiceContext，下游 NRE 静默崩溃。
    /// 修复后：守卫 5 拦截，BuildAndReportErrorAsync 返回错误结果。
    /// </summary>
    [Fact]
    public async Task D_动作执行_Plug定义缺失_应返回错误且不抛NRE()
    {
        // Arrange
        var request = BuildActionRequest();
        var plugData = BuildPlugData();
        _plugDataService.GetByPlugDefinitionIdAsync(Arg.Any<string>()).Returns(plugData);
        // 关键：plug == null
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns((Plug?)null);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ExecuteStatus.Should().Be(JobStatus.完成);
        erd.ExecuteSubStatus.Should().Be(JobSubStatus.出错);
    }

    #endregion

    #region E. 动作执行 + plug!=null + handler==null

    /// <summary>
    /// 场景 E: 动作执行分支，plug 有但 handler 找不到。
    /// 修复前：handler?.PlugCommonExecute 中 handler 为 null 报 NRE。
    /// 修复后：守卫 6 拦截，BuildAndReportErrorAsync 返回错误结果。
    /// </summary>
    [Fact]
    public async Task E_动作执行_Handler缺失_应返回错误且不抛NRE()
    {
        // Arrange
        var request = BuildActionRequest();
        var plugData = BuildPlugData();
        var plug = BuildPlug();
        _plugDataService.GetByPlugDefinitionIdAsync(Arg.Any<string>()).Returns(plugData);
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns(plug);
        _handlerService.GetExecuteHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);
        _handlerService.GetCategoryFallbackHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ExecuteStatus.Should().Be(JobStatus.完成);
        erd.ExecuteSubStatus.Should().Be(JobSubStatus.出错);
    }

    #endregion

    #region F. 动作执行 + 正常路径

    /// <summary>
    /// 场景 F: 动作执行正常路径，handler.PlugCommonExecute 被调用一次。
    /// </summary>
    [Fact]
    public async Task F_动作执行_正常路径_应调用Handler_PlugCommonExecute_一次()
    {
        // Arrange
        var request = BuildActionRequest();
        var plugData = BuildPlugData();
        var plug = BuildPlug();
        var handler = Substitute.For<IPlugCommonExecute>();
        var expectedResult = new ExecuteResultData
        {
            ExecuteStatus = JobStatus.完成,
            ExecuteSubStatus = JobSubStatus.已完成,
            ResultString = "F-success"
        };
        handler.PlugCommonExecute(Arg.Any<ExecuteServiceContext>()).Returns(Task.FromResult<ExecuteResultData?>(expectedResult));

        _plugDataService.GetByPlugDefinitionIdAsync(Arg.Any<string>()).Returns(plugData);
        _plugManageService.GetPlugByDefinitionId(Arg.Any<string>()).Returns(plug);
        _handlerService.GetExecuteHandler(Arg.Any<string>()).Returns(handler);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ResultString.Should().Be("F-success");
        await handler.Received(1).PlugCommonExecute(Arg.Any<ExecuteServiceContext>());
    }

    #endregion

    #region G. 无插头ID + plug==null (原有逻辑回归)

    /// <summary>
    /// 场景 G: 无插头ID（PlugDefinitionId 为空），通过 PlugTypeKey 找不到 Plug。
    /// 原代码 88-100 行已处理。验证 BugFix 没有破坏此路径。
    /// </summary>
    [Fact]
    public async Task G_无插头ID_PlugTypeKey找不到Plug_应返回错误()
    {
        // Arrange
        var request = new PlugExecutionRequest
        {
            PlugTypeKey = "UnknownTypeKey",
            ExecuteMode = ExecuteMode.Standalone,
            ExecuteResultData = new ExecuteResultData
            {
                Ids = new ExecuteIdsBundle
                {
                    PlugDefinitionId = null,
                    ExecuteTaskPlugIds = new List<string>() // Count==0 走首次执行
                }
            }
        };
        _plugManageService.GetPlugByTypeName(Arg.Any<string>()).Returns((Plug?)null);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ExecuteStatus.Should().Be(JobStatus.完成);
        erd.ExecuteSubStatus.Should().Be(JobSubStatus.出错);
    }

    #endregion

    #region H. 无插头ID + handler==null (Standalone)

    /// <summary>
    /// 场景 H: 无插头ID + Standalone + handler 找不到。
    /// 修复前：handler?.PlugCommonExecute 中 handler 为 null 报 NRE。
    /// 修复后：守卫 2 拦截，BuildAndReportErrorAsync 返回错误结果。
    /// </summary>
    [Fact]
    public async Task H_无插头ID_Standalone_Handler缺失_应返回错误且不抛NRE()
    {
        // Arrange
        var request = new PlugExecutionRequest
        {
            PlugTypeKey = TestPlugTypeKey,
            ExecuteMode = ExecuteMode.Standalone,
            ExecuteResultData = new ExecuteResultData
            {
                Ids = new ExecuteIdsBundle
                {
                    PlugDefinitionId = null,
                    ExecuteTaskPlugIds = new List<string>()
                }
            }
        };
        var plug = BuildPlug();
        _plugManageService.GetPlugByTypeName(Arg.Any<string>()).Returns(plug);
        _handlerService.GetExecuteHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);
        _handlerService.GetCategoryFallbackHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ExecuteStatus.Should().Be(JobStatus.完成);
        erd.ExecuteSubStatus.Should().Be(JobSubStatus.出错);
    }

    #endregion

    #region I. 无插头ID + handler==null (非 Standalone)

    /// <summary>
    /// 场景 I: 无插头ID + 非 Standalone + handler 找不到。
    /// 修复前：handler?.PlugCommonExecute 中 handler 为 null 报 NRE。
    /// 修复后：守卫 1 拦截，BuildAndReportErrorAsync 返回错误结果。
    /// </summary>
    [Fact]
    public async Task I_无插头ID_非Standalone_Handler缺失_应返回错误且不抛NRE()
    {
        // Arrange
        var request = new PlugExecutionRequest
        {
            PlugTypeKey = TestPlugTypeKey,
            ExecuteMode = ExecuteMode.Plug, // 非 Standalone
            ExecuteResultData = new ExecuteResultData
            {
                Ids = new ExecuteIdsBundle
                {
                    PlugDefinitionId = null,
                    ExecuteTaskPlugIds = new List<string>()
                }
            }
        };
        var plug = BuildPlug();
        _plugManageService.GetPlugByTypeName(Arg.Any<string>()).Returns(plug);
        _handlerService.GetExecuteHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);
        _handlerService.GetCategoryFallbackHandler(Arg.Any<string>()).Returns((IPlugCommonExecute?)null);

        // Act
        var result = await _engine.StartExecutePlug(request);

        // Assert
        result.Should().NotBeNull();
        var erd = (ExecuteResultData)result!;
        erd.ExecuteStatus.Should().Be(JobStatus.完成);
        erd.ExecuteSubStatus.Should().Be(JobSubStatus.出错);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 构造"首次执行"请求：ExecuteTaskPlugIds 为空，PlugDefinitionId 有值。
    /// </summary>
    private static PlugExecutionRequest BuildFirstTimeRequest()
    {
        return new PlugExecutionRequest
        {
            PlugTypeKey = TestPlugTypeKey,
            ExecuteMode = ExecuteMode.Plug,
            ExecuteResultData = new ExecuteResultData
            {
                Ids = new ExecuteIdsBundle
                {
                    PlugDefinitionId = TestPlugDefinitionId,
                    PDZId = "test-pdz-001",
                    ExecuteTaskPlugIds = new List<string>() // Count==0 触发"流程图来源"分支
                }
            }
        };
    }

    /// <summary>
    /// 构造"动作执行"请求：ExecuteTaskPlugIds 非空。
    /// </summary>
    private static PlugExecutionRequest BuildActionRequest()
    {
        return new PlugExecutionRequest
        {
            PlugTypeKey = TestPlugTypeKey,
            ExecuteMode = ExecuteMode.Action,
            ExecuteResultData = new ExecuteResultData
            {
                Ids = new ExecuteIdsBundle
                {
                    PlugDefinitionId = TestPlugDefinitionId,
                    PDZId = "test-pdz-001",
                    ExecuteTaskPlugIds = new List<string> { $"{TestPlugDefinitionId}|action-1" } // 非空触发"动作执行"分支
                }
            }
        };
    }

    private static PlugData BuildPlugData() => new()
    {
        Id = 1,
        PlugDefinitionId = TestPlugDefinitionId,
        Name = TestPlugName,
        PlugTypeKey = TestPlugTypeKey,
        Category = "桌面类_自研",
        OnlyExecuteAction = false,
    };

    private static Plug BuildPlug() => new()
    {
        Id = 1,
        DefinitionId = TestPlugDefinitionId,
        Name = TestPlugName,
        PlugTypeKey = TestPlugTypeKey,
        Category = "桌面类_自研",
    };

    #endregion
}
