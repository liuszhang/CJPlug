<#
.SYNOPSIS
    CJPlug 发布根组装（方案《CJPlug 发布纳入 CJSuite 与 AppHost 自带 DCP 方案》§4.4）。

.DESCRIPTION
    把 CJPlug 的构建产物按**白名单**组装成发布根，供 CJSuite.Pack 以 asset 型组件（sourceDir）收包。

    产出（均相对 CJPlug 根）：

        05.Release\main\       → cjplug 组件载荷（服务器形态）
                                  CJCore.AspireHost.AppHost\<Cfg>\<tfm>\   （含 aspire\dcp、aspire\dashboard）
                                  CJCore.AppHost.Manager\<Cfg>\net10.0-windows\（服务管理壳，与 AppHost 同根并排）
                                  Services\<Cfg>\net10.0\ 、net10.0-windows\
                                  PlugConfig\                               （运行时真源，含 UserPlugs.xml）
        05.Release\desktop\    → cjplug-desktop 组件载荷（个人形态）
                                  CJ.Plug.Desktop\<Cfg>\net10.0-windows\
        05.Release\station\    → station-settingui 组件载荷（图站形态，独立 publish 三件套）
                                  Agent\ 、ApiServer\ 、SettingUI\

    设计约束（方案锁定项）：
      · 枝 2  ：不自带 dcptun_c / _manifest（由 AppHost 项目的构建目标控制）。
      · 枝 7  ：只收 Release 单配置；**不收** CJ.Plug.Setup、**不收** Debug。
      · 枝 8  ：组装后断言发布根内 dcp.exe 与 Aspire.Dashboard.exe 存在（护栏）。
      · 枝 13 ：黑名单排除运行期残留（*.db / *.log / StationLogs / App_Data …）+ 组装后断言；
                另按 PackageService 既有做法排除 *.pdb。
      · 枝 18 ：图站三件套必须**独立 publish**（8 个服务共享同一输出目录，切不出小载荷）。
      · D8    ：图站载荷保持 <根>\{Agent,ApiServer,SettingUI} 形状（与旧 CJ.Plug.StationSetup 期望一致）。
      · Target 语义（2026-10-07 一次修正，原 D9 见方案 §9.6；**构建粒度**二次修正见 §9.8）：
                -Target main    → 构建整解（铺满共享 Services 目录）+ **只**组装 main（cjplug 组件的命令）
                -Target desktop → **只**构建 CJ.Plug.Desktop 的闭包 + **只**组装 desktop（cjplug-desktop 组件的命令）
                -Target station → 只组装 station（三件套各自 publish，本就不需要整解构建）
                -Target all     → 构建整解一次 + 三者全组装（本地一键）
                为什么要把 main/desktop 拆开：
                  ① 组装（§9.6）：避免"打 cjplug 与打 cjplug-desktop 各付一次全量组装
                     （≈1.25GB 拷贝 + pdb 扫描，实测重复一遍约 56s）"；
                  ② 构建（§9.8）：desktop 载荷只含 CJ.Plug.Desktop\Release\net10.0-windows，
                     该工程唯一 ProjectReference 是 CJ.Plug.LicenseApiClient（闭包 4 个工程，实测）；
                     而 AppHost 是用 AddExecutable("dotnet", servicesDir, xxx.dll) 拉起服务的，
                     所以"整解构建"对**共享 Services 目录**是必需的、但对 desktop 是纯浪费
                     （实测：整解构建空转一次 ≈47s，紧接别的构建活动时还会连带重编一批工程）。

.NOTES
    退出码：0 = 成功；非 0 = 失败（CJSuite.Pack 的「前置构建」按非 0 中止出包）。
    本脚本**幂等**：每次先清空目标 staging 目录再组装。
#>
[CmdletBinding()]
param(
    [ValidateSet('main', 'desktop', 'station', 'all')]
    [string]$Target = 'all',

    [string]$Configuration = 'Release',

    # 跳过 dotnet build（只重组装，用于快速迭代）
    [switch]$SkipBuild,

    # 跳过图站三件套的 dotnet publish（用于只出 main/desktop）
    [switch]$SkipStationPublish
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# ── 控制台输出编码：钉死 UTF-8（2026-10-07 实测，勿删） ────────────────────────────────
# 本脚本常被别的脚本 / 打包器以「重定向 stdout（进程无控制台）」的方式拉起；此时 PowerShell
# 会按**系统 OEM 代码页**（本机 936/GBK）写中文，而调用方按 UTF-8 解码
# ⇒ 中文全变 U+FFFD（在 GUI 里渲染成 ◆ 菱形）。原始字节证据：`=== D7E9D7B0 main ...`（GBK 的"组装"）。
# 实测两种修法：① 本行（显式设置）→ 输出 UTF-8 ✔；② 只设环境变量 DOTNET_SYSTEM_CONSOLE_DEFAULT_ENCODING=utf-8 → 仍 GBK ✘。
# 调用方口径见 CJSuite.Pack\Services\BuildOrchestrator.PreBuild.cs（按 UTF-8 解码子进程输出）。
try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false) } catch { }

# ── 路径：以本脚本位置推 CJPlug 根（05.Build 的上一级） ────────────────────────────────
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publishDir = Join-Path $repoRoot '02.Publish'
$releaseRoot = Join-Path $repoRoot '05.Release'
$sln = Join-Path $repoRoot 'CJ.Plug-Aspire.sln'
# desktop 载荷的**唯一**来源工程（2026-10-07 §9.8）：desktop 组件的前置构建只 build 这一条闭包，
# 不再拉整解（CJ.Plug.Desktop 唯一 ProjectReference = CJ.Plug.LicenseApiClient）。
$desktopProj = Join-Path $repoRoot 'src\WebHost\CJ.Plug.Desktop\CJ.Plug.Desktop.csproj'

# AppHost 与服务管理壳（2026-10-29《CJCore AppHost 服务管理壳方案》）：
#   两者源码都在**兄弟仓 CJCore**。AppHost 输出由其 csproj 的 BaseOutputPath 钉在 CJPlug\02.Publish 下；
#   管理壳已改走默认 bin（2026-10-29 简化），发布落点由下方构建命令显式 -o。
#   跨仓相对路径是已知代价（方案 §7 D-2 已登记）：CJPlug 目录布局调整时需同步这里。
$appHostProj = Join-Path $repoRoot '..\CJCore\src\Apps\CJCore.AspireHost.AppHost\CJCore.AspireHost.AppHost.csproj'
$managerProj = Join-Path $repoRoot '..\CJCore\src\Apps\CJCore.AppHost.Manager\CJCore.AppHost.Manager.csproj'

if (-not (Test-Path $sln)) { throw "未找到解决方案：$sln（本脚本必须位于 <CJPlug>\05.Build\ 下）" }

# ── 黑名单（枝 13）：运行期残留 + 调试符号 ────────────────────────────────────────────
# ⚠ 两类要区分开（2026-10-07 实测教训）：
#   ①「运行期残留」出现即**中止出包**（*_db/_log 是污染：曾实测把开发机的 station_tasks.db 与
#     StationLogs\log2026*.txt 打进载荷，装到客户机就是拿开发数据覆盖人家数据）；
#     `elsa-signing.key*` 同属此类：它是服务**首次启动自愈生成**的 JWT 签名密钥（方案 §9.7），
#     开发机跑过一次就会落在服务目录里 —— 若随包分发，等于把同一把密钥发给所有客户（可互相伪造 token）。
#   ②「调试符号 *pdb」是 publish 的正常副产物，**静默剔除**即可（PackageService 既有做法也是排除 pdb）——
#     若一并当断言失败，图站三件套 publish 会因 161 个 .pdb 直接打不出包。
$excludeFilePatterns = @('*.db', '*.db-shm', '*.db-wal', '*.log', 'elsa-signing.key*', 'window-state.json',
    # 2026-10-29《CJCore AppHost 服务管理壳方案》R5：用户覆盖配置是**本机运行期产物**，
    #   开发机上改过一次端口就会落在 AppHost 输出目录里。若随包分发，会把开发者的
    #   本机绝对路径（如 D:\Deploy\...）带到客户机 —— 与 *.db / elsa-signing.key 同属污染。
    #   除 robocopy /XF 排除外，Assert-Clean 会做**反向断言**（发布包内不存在该文件）。
    'apphost.user.json')
# 2026-10-07 追加两类运行期残留（实测证据见方案 §9.8）：
#   · window-state.json       —— 桌面端**窗口位置/大小**持久化文件，开发机跑过一次就会落在输出目录里；
#   · *.exe.WebView2\         —— WebView2 的用户数据/缓存目录，实测 1073 文件 / 383MB（desktop 载荷 407MB 的大头），
#                                装到客户机就是把开发机的浏览缓存与 Cookie 发出去（同 *.db / StationLogs 一类污染）。
$excludeDirNames = @('StationLogs', 'Logs', 'App_Data', '.vs', 'obj', 'bin', 'CJ.Plug.Desktop.exe.WebView2',
    # 2026-10-29：管理壳的 WebView2 用户数据目录。壳已把 UserDataFolder 指到
    #   %LOCALAPPDATA%\CJ\AppHostManager\WebView2（不落安装目录），这里仍列进黑名单做双保险 ——
    #   一旦将来有人改回默认位置，Assert-Clean 会当场拦下，而不是把浏览缓存发到客户机。
    'CJCore.AppHost.Manager.exe.WebView2')
$stationStandaloneExe = @('CJ.Plug.StationAgent.exe', 'CJ.Plug.StationApiServer.exe')
$stripFilePatterns = @('*.pdb')

function Write-Step([string]$msg) {
    Write-Host ''
    Write-Host "=== $msg ===" -ForegroundColor Cyan
}

function Invoke-Dotnet([string]$desc, [string[]]$dotnetArgs) {
    Write-Host "[$desc] dotnet $($dotnetArgs -join ' ')" -ForegroundColor DarkGray
    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) { throw "$desc 失败（exit=$LASTEXITCODE）：dotnet $($dotnetArgs -join ' ')" }
}

function Reset-Staging([string]$path) {
    # 防误删：只允许清理位于 <CJPlug>\05.Release\ 之下的目录，且必须解析为绝对路径
    $full = [System.IO.Path]::GetFullPath($path)
    $rootFull = [System.IO.Path]::GetFullPath($releaseRoot)
    if (-not $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "拒绝清理非发布根下的目录：$full"
    }
    if (Test-Path $full) { Remove-Item -LiteralPath $full -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $full | Out-Null
}

function Copy-Tree([string]$src, [string]$dst) {
    if (-not (Test-Path $src)) { throw "源目录不存在：$src（请确认已构建对应项目）" }
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    # robocopy 退出码 0..7 均为成功（位标志），>=8 才是失败
    # 2026-10-07：黑名单目录改用 robocopy /XD **源头排除**（此前只在拷完后 Remove-ExcludedDirs 删），
    #   净效果等价（都是"任何层级的同名目录都不要"），但省掉整份拷贝 —— 实测 desktop 载荷里
    #   383MB 的 CJ.Plug.Desktop.exe.WebView2 不必先拷 407MB 再删。
    $rcArgs = @($src, $dst, '/E', '/R:1', '/W:1', '/NFL', '/NDL', '/NJH', '/NJS', '/NP', '/XD') + $excludeDirNames + @('/XF') + $excludeFilePatterns
    & robocopy @rcArgs | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy 失败（exit=$LASTEXITCODE）：$src → $dst" }
    $global:LASTEXITCODE = 0
}

function Remove-ExcludedDirs([string]$stagingRoot) {
    foreach ($name in $excludeDirNames) {
        Get-ChildItem -LiteralPath $stagingRoot -Directory -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq $name } |
            ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

function Remove-DebugSymbols([string]$stagingRoot) {
    # 剔除调试符号（*.pdb）：publish/build 的正常副产物，体积与现场无用途（对齐 PackageService 的既有排除）。
    $removed = 0
    foreach ($pat in $stripFilePatterns) {
        Get-ChildItem -LiteralPath $stagingRoot -File -Recurse -Force -Filter $pat -ErrorAction SilentlyContinue |
            ForEach-Object {
                Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
                $removed++
            }
    }
    if ($removed -gt 0) { Write-Host "  [strip] 已剔除调试符号 $removed 个（$($stripFilePatterns -join ', ')）" }
}

function Assert-Paths([string]$stagingRoot, [string[]]$relativePaths) {
    $missing = @()
    foreach ($rel in $relativePaths) {
        $full = Join-Path $stagingRoot $rel
        if (-not (Test-Path $full)) { $missing += $rel }
    }
    if ($missing.Count -gt 0) {
        throw "组装后断言失败（关键入口缺失）：`n  - $(($missing -join "`n  - "))`n（staging: $stagingRoot）"
    }
}

function Assert-Clean([string]$stagingRoot) {
    $bad = @()
    foreach ($pat in ($excludeFilePatterns + $stripFilePatterns)) {
        $bad += Get-ChildItem -LiteralPath $stagingRoot -File -Recurse -Force -Filter $pat -ErrorAction SilentlyContinue
    }
    foreach ($name in $excludeDirNames) {
        $bad += Get-ChildItem -LiteralPath $stagingRoot -Directory -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq $name }
    }
    if ($bad.Count -gt 0) {
        $list = ($bad | Select-Object -First 20 | ForEach-Object { $_.FullName }) -join "`n  - "
        throw "组装后断言失败（载荷不洁净，出现黑名单命中 $($bad.Count) 项）：`n  - $list"
    }
}

# P9.1（2026-10-30，D2 = B「回退形态一次全删」）：正向断言管"必须存在"，负向断言管"必须消失"。
#   只做正向断言会漏掉一类静默回归：某个面又被改回 OutputType=Exe，exe 重新进包 —— 此时
#   正向断言照样通过（dll 也在），包体变胖、装配入口重新分裂，而没人会注意到。
# P9.1（2026-10-30）：Services\<Cfg>\net10.0 是**共享输出目录**（全部工程都往这里 build），
#   于是 main 载荷会连带收进图站三件套的 exe —— 但它们有自己的独立 publish
#   （-Target station 输出到 station\{Agent,ApiServer,SettingUI}，见 §9.8/D8），
#   放在 main 里是纯冗余，且这些 exe 正是"回退形态"容易被误起的那批文件。
function Remove-StationStandaloneExe([string]$stagingRoot, [string]$cfg) {
    $dir = Join-Path $stagingRoot "Services\$cfg"
    if (-not (Test-Path $dir)) { return }
    $n = 0
    foreach ($tfm in @('net10.0', 'net10.0-windows')) {
        $d = Join-Path $dir $tfm
        if (-not (Test-Path $d)) { continue }
        foreach ($name in $stationStandaloneExe) {
            $f = Join-Path $d $name
            if (Test-Path $f) { Remove-Item -LiteralPath $f -Force; $n++ }
        }
    }
    if ($n -gt 0) { Write-Host "  [strip] 已从 main 载荷剔除图站独立 exe $n 个（station 三件套自有独立 publish）" }
}

function Assert-Absent([string]$stagingRoot, [string[]]$relativePaths, [string]$reason) {
    $found = @()
    foreach ($rel in $relativePaths) {
        $full = Join-Path $stagingRoot $rel
        if (Test-Path $full) { $found += $rel }
    }
    if ($found.Count -gt 0) {
        throw "组装后断言失败（$reason，不应出现 $($found.Count) 项）：`n  - $(($found -join "`n  - "))`n（staging: $stagingRoot）"
    }
}

function Get-TreeSizeMB([string]$path) {
    if (-not (Test-Path $path)) { return 0 }
    $sum = (Get-ChildItem -LiteralPath $path -File -Recurse -Force -ErrorAction SilentlyContinue |
            Measure-Object -Property Length -Sum).Sum
    return [math]::Round($sum / 1MB, 1)
}

# ══════════════════════════════════════════════════════════════════════════════════════
Write-Host "CJPlug 发布根组装" -ForegroundColor Green
Write-Host "  CJPlug 根   : $repoRoot"
Write-Host "  配置        : $Configuration"
Write-Host "  目标        : $Target"

# ── ⓪ 构建（**按 Target 拆**，2026-10-07 二次修正见方案 §9.8） ────────────────────────────
# main：**必须**整解构建。AppHost 用 AddExecutable("dotnet", servicesDir, xxx.dll) 拉起服务，而
#       DispatchServer / HostWebServer 都**不是** AppHost 的 ProjectReference ⇒
#       共享目录 02.Publish\Services 靠"整解全量构建"铺满，收窄会打不出完整载荷。
# desktop：载荷只有 CJ.Plug.Desktop\Release\net10.0-windows（闭包 4 个工程）⇒ 只 build 该工程，
#       与整解 165 个 src 工程彻底解耦（原实现让 desktop 也跑整解：实测空转一次 ≈47s）。
# station：三件套走独立 publish（见 ③），不需要整解构建。
# -Target all：整解只构建一次（desktop 块不再重复 build）。
$slnBuilt = $false
if ($Target -in @('main', 'all') -and -not $SkipBuild) {
    Write-Step "构建解决方案（$Configuration）"
    Invoke-Dotnet 'build solution' @('build', $sln, '-c', $Configuration, '-v:m', '-nologo')
    $slnBuilt = $true

    # 2026-10-29：AppHost 与服务管理壳的**源码在 CJCore 仓**，不在 CJ.Plug-Aspire.sln 里
    #   （《Aspire 编排壳迁移 CJCore》把它们移出 CJPlug 解决方案），所以上面的整解构建覆盖不到这两个工程。
    #   之前一直没暴露，是因为开发机上它们已被单独构建过；换台干净机器直接 -Target main 就会
    #   在 Copy-Tree 处以「源目录不存在」失败。
    Write-Step "构建 CJCore 侧编排器与管理壳（$Configuration）"
    if (-not (Test-Path $appHostProj)) { throw "未找到工程：$appHostProj" }
    if (-not (Test-Path $managerProj)) { throw "未找到工程：$managerProj" }
    # AppHost：输出落点仍由其 csproj 的 BaseOutputPath 钉在 02.Publish（它必须与 Services 同根并排）。
    Invoke-Dotnet 'build CJCore.AspireHost.AppHost' @('build', $appHostProj, '-c', $Configuration, '-v:m', '-nologo')
    # 管理壳（2026-10-29 简化）：壳 csproj 不再有 BaseOutputPath（输出走默认 bin），
    #   发布落点改由本脚本显式 -o —— 与 AppHost 同根并排的硬要求不变（AppHostLocator 上溯三级定位）。
    $managerOut = Join-Path $publishDir "CJCore.AppHost.Manager\$Configuration\net10.0-windows"
    Invoke-Dotnet 'build CJCore.AppHost.Manager' @('build', $managerProj, '-c', $Configuration, '-o', $managerOut, '-v:m', '-nologo')
}

# ── ① main staging（cjplug：服务器形态；枝 7 = 只收 Release 单配置） ────────────────────
# 2026-10-07 修正（原 D9 见方案 §9.6）：Target 拆分，本块**只**组装 main。
#   原实现里 -Target main / -Target desktop 走同一块、都同时组装两份 ⇒ 打 cjplug 与
#   打 cjplug-desktop 各付一次全量（≈1.25GB 拷贝 + pdb 扫描，实测重复一遍约 56s）。
if ($Target -in @('main', 'all')) {
    Write-Step "组装 main（服务器形态：AppHost + Services + PlugConfig）"

    $mainStaging = Join-Path $releaseRoot 'main'
    Reset-Staging $mainStaging | Out-Null

    # cjplug（服务器形态）：AppHost（含自带 DCP/Dashboard）+ Services + PlugConfig
    Copy-Tree (Join-Path $publishDir "CJCore.AspireHost.AppHost\$Configuration\net10.0") (Join-Path $mainStaging "CJCore.AspireHost.AppHost\$Configuration\net10.0")
    # 服务管理壳（2026-10-29 新增）：与 AppHost **同根并排**是硬要求 ——
    #   壳按「自身目录上溯三级 = 发布根」定位 AppHost（AppHostLocator），错位就找不到 dll。
    #   体积约 4MB，故并入 main 载荷而不单开组件（服务器形态用不到 GUI 也不影响服务运行）。
    Copy-Tree (Join-Path $publishDir "CJCore.AppHost.Manager\$Configuration\net10.0-windows") (Join-Path $mainStaging "CJCore.AppHost.Manager\$Configuration\net10.0-windows")
    Copy-Tree (Join-Path $publishDir "Services\$Configuration\net10.0") (Join-Path $mainStaging "Services\$Configuration\net10.0")
    Copy-Tree (Join-Path $publishDir "Services\$Configuration\net10.0-windows") (Join-Path $mainStaging "Services\$Configuration\net10.0-windows")
    Copy-Tree (Join-Path $publishDir 'PlugConfig') (Join-Path $mainStaging 'PlugConfig')

    Remove-ExcludedDirs $mainStaging
    Remove-DebugSymbols $mainStaging
    Remove-StationStandaloneExe $mainStaging $Configuration

    Assert-Paths $mainStaging @(
        "CJCore.AspireHost.AppHost\$Configuration\net10.0\CJCore.AspireHost.AppHost.dll",
        "CJCore.AspireHost.AppHost\$Configuration\net10.0\aspire\dcp\dcp.exe",
        "CJCore.AspireHost.AppHost\$Configuration\net10.0\aspire\dashboard\Aspire.Dashboard.exe",
        "CJCore.AppHost.Manager\$Configuration\net10.0-windows\CJCore.AppHost.Manager.exe",
        # P9（2026-10-30，D1 = A）：宿主 exe 由 CJ.Plug.ApiServer 改为 CJPlug.Server。
        # ⚠ 断言的必须是**可执行宿主**，不是同名 dll 是否存在 —— ApiServer 降级为类库后
        #   它的 dll 仍会随宿主进目录（这是正常的），若继续断言旧 dll 等于永远为真、失去护栏意义。
        "Services\$Configuration\net10.0\CJPlug.Server.dll",
        # 配置归一（2026-10-30）：主服务的配置只有执行目录下的 appsettings.json 一份。
        # 必须断言它存在 —— 它是连接串 / InternalApi / 三员种子密码的唯一载体，
        # 缺了服务仍能起来但会连错库（历史上就是这样静默坏掉的）。
        "Services\$Configuration\net10.0\appsettings.json",
        # 工位机是独立进程，配置走专属文件名（唯一保留的专属配置）。
        "Services\$Configuration\net10.0\appsettings.CJ.Plug.StationApiServer.json",
        "Services\$Configuration\net10.0-windows\CJ.Plug.ExecuteApp.exe",
        'PlugConfig\UserPlugs.xml'
    )
    # P9.1（2026-10-30，D2 = B）：三个 referred 项目已类库化，包里不许再出现旧的多进程 exe。
    Assert-Absent $mainStaging @(
        "Services\$Configuration\net10.0\CJ.Plug.ApiServer.exe",
        "Services\$Configuration\net10.0\CJ.Plug.DispatchServer.exe",
        "Services\$Configuration\net10.0\CJ.Plug.McpServer.exe",
        "Services\$Configuration\net10.0\CJ.Plug.HostWebServer.exe",
        "Services\$Configuration\net10.0\CJ.Plug.StationAgent.exe",
        "Services\$Configuration\net10.0\CJ.Plug.StationApiServer.exe",
        # 2026-10-30：CJ.Plug.AIChat 是 UI 组件库（被 CJ.Plug.AI 引用），曾被传递复制成 exe。
        "Services\$Configuration\net10.0\CJ.Plug.AIChat.exe"
    ) '回退形态 / 图站独立 / UI 组件 exe 应已全部下线'
    # 配置归一（2026-10-30）：类库化项目的专属配置不再生成 —— 它们没有自己的进程，
    # 留着会让人以为还有独立服务在跑，也会掩盖「根 appsettings.json 被覆盖」这类故障。
    Assert-Absent $mainStaging @(
        "Services\$Configuration\net10.0\appsettings.CJ.Plug.ApiServer.json",
        "Services\$Configuration\net10.0\appsettings.CJ.Plug.DispatchServer.json",
        "Services\$Configuration\net10.0\appsettings.CJ.Plug.McpServer.json",
        "Services\$Configuration\net10.0\appsettings.CJ.Plug.HostWebServer.json",
        "Services\$Configuration\net10.0\appsettings.CJPlug.Server.json"
    ) '配置归一后不应再出现类库项目的专属配置'
    Assert-Clean $mainStaging

    Write-Host ("  main\    : {0} MB" -f (Get-TreeSizeMB $mainStaging)) -ForegroundColor Green
}

# ── ② desktop staging（cjplug-desktop：个人形态，仅桌面端） ────────────────────────────
if ($Target -in @('desktop', 'all')) {
    Write-Step "组装 desktop（个人形态：仅 CJ.Plug.Desktop）"

    # 构建粒度（§9.8）：只 build desktop 自己的闭包。`-Target all` 时整解已构建（$slnBuilt），此处跳过。
    if (-not $SkipBuild -and -not $slnBuilt) {
        if (-not (Test-Path $desktopProj)) { throw "未找到桌面前端工程：$desktopProj" }
        Invoke-Dotnet 'build desktop' @('build', $desktopProj, '-c', $Configuration, '-v:m', '-nologo')
    }

    $desktopStaging = Join-Path $releaseRoot 'desktop'
    Reset-Staging $desktopStaging | Out-Null

    Copy-Tree (Join-Path $publishDir "CJ.Plug.Desktop\$Configuration\net10.0-windows") (Join-Path $desktopStaging "CJ.Plug.Desktop\$Configuration\net10.0-windows")

    Remove-ExcludedDirs $desktopStaging
    Remove-DebugSymbols $desktopStaging

    Assert-Paths $desktopStaging @("CJ.Plug.Desktop\$Configuration\net10.0-windows\CJ.Plug.Desktop.exe")
    Assert-Clean $desktopStaging

    Write-Host ("  desktop\ : {0} MB" -f (Get-TreeSizeMB $desktopStaging)) -ForegroundColor Green
}

# ── ③ station staging（枝 18/D8：三件套必须独立 publish） ─────────────────────────────
if ($Target -in @('station', 'all')) {
    Write-Step "组装 station（图站形态：Agent + ApiServer + SettingUI）"

    $stationStaging = Join-Path $releaseRoot 'station'
    Reset-Staging $stationStaging | Out-Null

    if (-not $SkipStationPublish) {
        # 注意：8 个服务共享 02.Publish\Services\<Cfg>\net10.0，**不能**靠切那棵树得到图站载荷。
        # 框架依赖发布（D7，与现状 02.Publish 一致），由清单的 dotnet-runtime 预检兜住运行时。
        Invoke-Dotnet 'publish Agent' @(
            'publish', (Join-Path $repoRoot 'src\PlugStation\CJ.Plug.StationAgent\CJ.Plug.StationAgent.csproj'),
            '-c', $Configuration, '-o', (Join-Path $stationStaging 'Agent'), '--nologo')
        Invoke-Dotnet 'publish ApiServer' @(
            'publish', (Join-Path $repoRoot 'src\PlugStation\CJ.Plug.StationApiServer\CJ.Plug.StationApiServer.csproj'),
            '-c', $Configuration, '-o', (Join-Path $stationStaging 'ApiServer'), '--nologo')
        Invoke-Dotnet 'publish SettingUI' @(
            'publish', (Join-Path $repoRoot 'src\PlugStation\StationSettingUI\StationSettingUI.csproj'),
            '-c', $Configuration, '-o', (Join-Path $stationStaging 'SettingUI'), '--nologo')
    }
    else {
        Write-Host '  （-SkipStationPublish：跳过三件套 publish）' -ForegroundColor Yellow
    }

    Remove-ExcludedDirs $stationStaging
    Remove-DebugSymbols $stationStaging
    Assert-Paths $stationStaging @(
        'Agent\CJ.Plug.StationAgent.exe',
        'ApiServer\CJ.Plug.StationApiServer.exe',
        'SettingUI\StationSettingUI.exe'
    )
    # 负向断言（2026-10-30）：CJ.Plug.AIChat 是 UI 组件库，曾被传递复制成 exe 混进图站载荷
    # （StationApiServer → ModuleConfig → CJ.Plug.AI → CJ.Plug.AIChat）。已类库化，这里守住不回退。
    Assert-Absent $stationStaging @(
        'ApiServer\CJ.Plug.AIChat.exe'
    ) '图站载荷不得混入库组件 exe（CJ.Plug.AIChat 已类库化）'
    Assert-Clean $stationStaging

    Write-Host ("  station\ : {0} MB" -f (Get-TreeSizeMB $stationStaging)) -ForegroundColor Green
}

Write-Host ''
Write-Host "发布根组装完成：$releaseRoot" -ForegroundColor Green
exit 0
