# 拆分方案：PlugExecuteService.cs

## 拆分方案

### 方案概览

| 新类名 | 文件路径 | 角色 | 包含方法 | 行数估计 |
|--------|----------|------|----------|----------|
| `PlugExecuteService` (改造) | `Services/PlugExecuteService.cs` | **门面类**，实现 `IPlugExecuteService`，组合内部服务 | `ExecuteMcpTool`, `GetExecutionStatus`, `ExecutePlug(string)` [Obsolete], `ExecutePlug(string, PlugExecutionRequest?)` [Obsolete], `StartExecutePlug`(委托), `ReportExecuteResult`(委托) | ~70 行 |
| `PlugExecutionEngine` | `Services/PlugExecutionEngine.cs` | **核心执行引擎**：插头启动 + 结果汇报的紧耦合循环 | `StartExecutePlug`, `ReportExecuteResult` | ~330 行 |
| `PlugBookmarkManager` | `Services/PlugBookmarkManager.cs` | **书签管理**：Outcome 存储 + Elsa 书签恢复 + 汇聚唤醒 | `ResumeBookmarkAsync`, `TryAwakenConvergencePlugs` | ~100 行 |
| `McpWorkflowRunner` | `Services/McpWorkflowRunner.cs` | **MCP 工作流执行子路径**：Use PDZ → Job PDZ → Elsa 流程图 | `ExecuteMcpWorkflowFromRequest` | ~90 行 |

> **行数估计说明**：原始有效代码 ~660 行（去掉 Obsolete 方法 150 行 + 空白/注释）。拆分后四个类合计约 ~590 行（含构造函数注入样板 ~30 行），比原来增加约 20 行样板代码，换来清晰的职责分离。

### 设计原则

- **零接口变更**：`IPlugExecuteService` 完全不改
- **零 DI 注册变更**：`Program.cs` / `Startup` 无需任何修改。三个新类为 `internal class`，由 `PlugExecuteService` 在构造函数中手动 `new` 并注入依赖
- **零业务逻辑变更**：每个方法体逐字复制，不做任何重构/优化
- **门面模式**：`PlugExecuteService` 保留所有 public 方法签名，Obsolete 方法和简单方法直接保留，`StartExecutePlug` / `ReportExecuteResult` 委托给 `PlugExecutionEngine`

---

### 依赖关系

```
PlugExecuteService (门面，实现 IPlugExecuteService)
  │
  ├── 直接持有 ──► PlugExecutionEngine (注入: PlugManageService, PlugExecuteHandlerService,
  │                   PlugDataService, PDZManageService, MainDbContext,
  │                   PlugBookmarkManager, McpWorkflowRunner)
  │
  ├── 直接持有 ──► PlugBookmarkManager (注入: MainApiClient)
  │                   ▲
  │                   │ 引用
  │                   │
  │              PlugExecutionEngine ──► PlugBookmarkManager
  │                   │                       ▲
  │                   │ 引用                   │ 自引用
  │                   ▼                       │
  │              McpWorkflowRunner     ResumeBookmarkAsync()
  │              (注入: PDZManageService,
  │               PlugManageService,
  │               MainApiClient,
  │               ElsaApiClient)
  │
  └── 直接持有 ──► ElsaApiClient (仅 GetExecutionStatus 使用)
```

**关键依赖链**：
1. `PlugExecuteService.StartExecutePlug()` → `PlugExecutionEngine.StartExecutePlug()`
2. `PlugExecutionEngine.StartExecutePlug()` → `McpWorkflowRunner.ExecuteMcpWorkflowFromRequest()` (MCP Workflow 分支)
3. `PlugExecuteService.ReportExecuteResult()` → `PlugExecutionEngine.ReportExecuteResult()`
4. `PlugExecutionEngine.ReportExecuteResult()` → `PlugBookmarkManager.ResumeBookmarkAsync()` / `TryAwakenConvergencePlugs()`
5. `PlugExecutionEngine.StartExecutePlug()` ⇄ `PlugExecutionEngine.ReportExecuteResult()` (动作链递归循环，保持不变)

---

### 各新类详细设计

#### 1. PlugExecuteService（门面类，改造后）

```csharp
public class PlugExecuteService : IPlugExecuteService
{
    private readonly PlugExecutionEngine _engine;
    private readonly PlugBookmarkManager _bookmarkManager;
    private readonly McpWorkflowRunner _mcpRunner;

    // 门面自身需要的依赖（Obsolete 方法、ExecuteMcpTool、GetExecutionStatus 使用）
    private readonly MainDbContext _dbContext;
    private IPlugManageService PlugManageService { get; }
    private IEnumerable<IPlugCommonExecute> PlugCommonExecutes { get; }
    private ElsaApiClient ElsaApiClient { get; }
    private IPlugExecuteHandlerService PlugExecuteHandlerService { get; }
    private IPDZManageService PDZManageService { get; }
    private IPlugDataService PlugDataService { get; }
    private MainApiClient MainApiClient { get; }
    private IElsaEngineService ElsaEngineService { get; }

    public PlugExecuteService(
        MainDbContext dbContext,
        IPlugManageService plugManageService,
        IPlugCommonExecute plugCommonExecute,
        IEnumerable<IPlugCommonExecute> plugCommonExecutes,
        IPlugExecuteHandlerService plugExecuteHandlerService,
        IPDZManageService pDZManageService,
        IPlugDataService plugDataService,
        MainApiClient mainApiClient,
        ElsaApiClient elsaApiClient,
        IElsaEngineService elsaEngineService)
    {
        _dbContext = dbContext;
        PlugManageService = plugManageService;
        PlugCommonExecutes = plugCommonExecutes;
        PlugExecuteHandlerService = plugExecuteHandlerService;
        PDZManageService = pDZManageService;
        PlugDataService = plugDataService;
        MainApiClient = mainApiClient;
        ElsaApiClient = elsaApiClient;
        ElsaEngineService = elsaEngineService;

        // 构造内部服务
        _bookmarkManager = new PlugBookmarkManager(mainApiClient);
        _mcpRunner = new McpWorkflowRunner(pDZManageService, plugManageService, mainApiClient, elsaApiClient);
        _engine = new PlugExecutionEngine(
            dbContext, plugManageService, plugExecuteHandlerService,
            plugDataService, pDZManageService,
            _bookmarkManager, _mcpRunner);
    }

    // 委托方法
    public Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request)
        => _engine.StartExecutePlug(request);

    public Task ReportExecuteResult(ExecuteResultData executeReport)
        => _engine.ReportExecuteResult(executeReport);

    // 直接保留的 public 方法（不变）
    public Task<string?> ExecuteMcpTool(McpToolExecutionRequest request) { /* 原样保留 */ }
    public Task<ExecutionStatusDto?> GetExecutionStatus(string workflowInstanceId) { /* 原样保留 */ }

    // Obsolete 方法（原样保留）
    [Obsolete("...")]
    public async Task<string?> ExecutePlug(string definitionId) { /* 不变 */ }

    [Obsolete("...")]
    public async Task<string?> ExecutePlug(string definitionId, PlugExecutionRequest? request) { /* 不变 */ }
}
```

#### 2. PlugExecutionEngine（核心执行引擎）

```csharp
internal class PlugExecutionEngine
{
    private readonly MainDbContext _dbContext;
    private readonly IPlugManageService _plugManageService;
    private readonly IPlugExecuteHandlerService _handlerService;
    private readonly IPlugDataService _plugDataService;
    private readonly IPDZManageService _pdzManageService;
    private readonly PlugBookmarkManager _bookmarkManager;
    private readonly McpWorkflowRunner _mcpRunner;

    public PlugExecutionEngine(
        MainDbContext dbContext,
        IPlugManageService plugManageService,
        IPlugExecuteHandlerService handlerService,
        IPlugDataService plugDataService,
        IPDZManageService pdzManageService,
        PlugBookmarkManager bookmarkManager,
        McpWorkflowRunner mcpRunner)
    {
        _dbContext = dbContext;
        _plugManageService = plugManageService;
        _handlerService = handlerService;
        _plugDataService = plugDataService;
        _pdzManageService = pdzManageService;
        _bookmarkManager = bookmarkManager;
        _mcpRunner = mcpRunner;
    }

    // StartExecutePlug — 原样从 PlugExecuteService 复制，内部调用改为:
    //   await PlugManageService.GetPlugByTypeName(...)     → await _plugManageService.GetPlugByTypeName(...)
    //   await PlugExecuteHandlerService.GetExecuteHandler  → await _handlerService.GetExecuteHandler(...)
    //   await PlugDataService.GetByPlugDefinitionIdAsync   → await _plugDataService.GetByPlugDefinitionIdAsync(...)
    //   await PDZManageService.GetByPDZId(...)             → await _pdzManageService.GetByPDZId(...)
    //   await PDZManageService.CreatePDZ(...)              → await _pdzManageService.CreatePDZ(...)
    //   await PDZManageService.DeletePDZ(...)              → await _pdzManageService.DeletePDZ(...)
    //   await ExecuteMcpWorkflowFromRequest(...)           → await _mcpRunner.ExecuteMcpWorkflowFromRequest(...)
    //   await ReportExecuteResult(...)                     → await ReportExecuteResult(...)  // 自身方法
    public async Task<ExecuteResultData?> StartExecutePlug(PlugExecutionRequest? request) { /* 原样 */ }

    // ReportExecuteResult — 原样复制，内部调用改为:
    //   await _dbContext.Set<BaseJob>().FirstOrDefaultAsync(...)
    //   await StartExecutePlug(...)                          → await StartExecutePlug(...)  // 自身方法
    //   await ResumeBookmarkAsync(...)                       → await _bookmarkManager.ResumeBookmarkAsync(...)
    //   await TryAwakenConvergencePlugs(...)                 → await _bookmarkManager.TryAwakenConvergencePlugs(...)
    public async Task ReportExecuteResult(ExecuteResultData executeReport) { /* 原样 */ }
}
```

#### 3. PlugBookmarkManager（书签管理）

```csharp
internal class PlugBookmarkManager
{
    private readonly MainApiClient _mainApiClient;

    public PlugBookmarkManager(MainApiClient mainApiClient)
    {
        _mainApiClient = mainApiClient;
    }

    // ResumeBookmarkAsync — 原样复制，内部调用改为:
    //   await MainApiClient.GetPDZByPDZIdAsync(...)     → await _mainApiClient.GetPDZByPDZIdAsync(...)
    //   await MainApiClient.CreateOrUpdatePDZ(...)      → await _mainApiClient.CreateOrUpdatePDZ(...)
    internal async Task ResumeBookmarkAsync(
        string? correlationId, string? plugId, string? pdzId, string[]? outcomes) { /* 原样 */ }

    // TryAwakenConvergencePlugs — 原样复制，内部调用改为:
    //   await MainApiClient.GetPDZByPDZIdAsync(...)     → await _mainApiClient.GetPDZByPDZIdAsync(...)
    //   await MainApiClient.CreateOrUpdatePDZ(...)      → await _mainApiClient.CreateOrUpdatePDZ(...)
    //   await ResumeBookmarkAsync(...)                   → await ResumeBookmarkAsync(...)  // 自身方法
    internal async Task TryAwakenConvergencePlugs(
        string? pdzId, string? jobCorrelationId) { /* 原样 */ }
}
```

#### 4. McpWorkflowRunner（MCP 工作流执行器）

```csharp
internal class McpWorkflowRunner
{
    private readonly IPDZManageService _pdzManageService;
    private readonly IPlugManageService _plugManageService;
    private readonly MainApiClient _mainApiClient;
    private readonly ElsaApiClient _elsaApiClient;

    public McpWorkflowRunner(
        IPDZManageService pdzManageService,
        IPlugManageService plugManageService,
        MainApiClient mainApiClient,
        ElsaApiClient elsaApiClient)
    {
        _pdzManageService = pdzManageService;
        _plugManageService = plugManageService;
        _mainApiClient = mainApiClient;
        _elsaApiClient = elsaApiClient;
    }

    // ExecuteMcpWorkflowFromRequest — 原样复制，内部调用改为:
    //   await PDZManageService.GetByPDZId(...)              → await _pdzManageService.GetByPDZId(...)
    //   await PlugManageService.GetPlugByDefinitionId(...)  → await _plugManageService.GetPlugByDefinitionId(...)
    //   await MainApiClient.SubmitExecute(...)              → await _mainApiClient.SubmitExecute(...)
    //   await MainApiClient.GetPDZByPDZIdAsync(...)         → await _mainApiClient.GetPDZByPDZIdAsync(...)
    //   await MainApiClient.CreateOrUpdatePDZ(...)          → await _mainApiClient.CreateOrUpdatePDZ(...)
    //   await ElsaApiClient.ExecuteWorkflowWithExecuteSetting(...) → await _elsaApiClient.ExecuteWorkflowWithExecuteSetting(...)
    internal async Task<ExecuteResultData?> ExecuteMcpWorkflowFromRequest(
        PlugExecutionRequest request, PlugData plugData) { /* 原样 */ }
}
```

---

### 风险点

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| **`StatusReporter` 静态调用** | 🟡 低 | `StatusReporter.ReportPlugStatus()` 和 `.CompleteActivityContext()` 是静态方法调用，不受类拆分影响 |
| **`CLog` 静态调用** | 🟡 低 | 同上，静态日志调用，无影响 |
| **`StartExecutePlug` ↔ `ReportExecuteResult` 递归循环** | 🟢 无 | 两者保留在同一类 `PlugExecutionEngine` 中，与原始行为完全一致 |
| **`PlugExecuteService` 构造函数膨胀** | 🟡 低 | 门面构造函数需要传入所有 10 个依赖并手动组装 3 个内部服务。可接受——这是 .NET 手动 DI 的常见模式 |
| **Obsolete 方法仍使用原始字段** | 🟡 低 | Obsolete 方法保留在门面中，使用门面的 `PlugManageService`、`PlugCommonExecutes` 等字段，不受影响 |
| **`ExecuteMcpTool` 调用 `this.StartExecutePlug`** | 🟢 无 | 现在调用 `_engine.StartExecutePlug()`，行为完全等价 |
| **合并冲突风险** | 🟡 中 | 如果其他分支同时修改了原始文件，会产生冲突。建议优先合入此重构 |

---

### 实现步骤

| 步骤 | 操作 | 依赖 |
|------|------|------|
| **S1** | 创建 `PlugBookmarkManager.cs`，从 `PlugExecuteService.cs` 剪切 `ResumeBookmarkAsync` + `TryAwakenConvergencePlugs`，替换字段引用 | 无 |
| **S2** | 创建 `McpWorkflowRunner.cs`，从 `PlugExecuteService.cs` 剪切 `ExecuteMcpWorkflowFromRequest`，替换字段引用 | 无 |
| **S3** | 创建 `PlugExecutionEngine.cs`，从 `PlugExecuteService.cs` 剪切 `StartExecutePlug` + `ReportExecuteResult`，替换字段引用为 `_bookmarkManager` / `_mcpRunner` / 私有字段 | S1, S2 |
| **S4** | 改造 `PlugExecuteService.cs` 构造函数：创建 `PlugBookmarkManager`、`McpWorkflowRunner`、`PlugExecutionEngine` 实例；`StartExecutePlug` / `ReportExecuteResult` 方法体替换为委托调用 | S1, S2, S3 |
| **S5** | 编译验证 + 回归测试确认行为不变 | S4 |

> **注意**：实际代码只需 4 个 commit（S1-S4 各一个），S5 是验证步骤而非代码变更。整个过程约 30 分钟。

---

### Shared Knowledge

- 所有新类使用 `internal` 访问修饰符，不暴露给外部程序集
- 三个内部类不实现任何接口，直接由 `PlugExecuteService` 在构造函数中 `new` 创建
- `IPlugExecuteService` 接口完全不改动
- `Program.cs` / DI 注册代码完全不改动
- 所有业务逻辑代码逐字复制，不做任何格式化、重命名或逻辑变更
- `StatusReporter`、`CLog`、`Log`、`ParameterGenerator` 等静态类引用无需更改
- 字段命名规则：内部类使用 `private readonly` 小驼峰命名（如 `_plugManageService`），门面类保持原有属性风格
