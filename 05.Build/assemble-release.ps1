<#
.SYNOPSIS
    CJPlug 发布根组装（方案《CJPlug 发布纳入 CJSuite 与 AppHost 自带 DCP 方案》§4.4）。

.DESCRIPTION
    把 CJPlug 的构建产物按**白名单**组装成发布根，供 CJSuite.Pack 以 asset 型组件（sourceDir）收包。

    产出（均相对 CJPlug 根）：

        05.Release\main\       → cjplug 组件载荷（服务器形态）
                                  CJ.Plug.AspireHost.AppHost\<Cfg>\<tfm>\   （含 aspire\dcp、aspire\dashboard）
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
      · Target 语义（2026-10-07 修正，原 D9 见方案 §9.6）：
                -Target main    → **只**组装 main（cjplug 组件的命令）
                -Target desktop → **只**组装 desktop（cjplug-desktop 组件的命令）
                -Target station → 只组装 station
                -Target all     → 三者全组装（本地一键）
                为什么要把 main/desktop 拆开：两个组件各声明自己那条最小命令，避免"打 cjplug 与打
                cjplug-desktop 各付一次全量组装（≈1.25GB 拷贝 + pdb 扫描，实测重复一遍约 56s）"。

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

if (-not (Test-Path $sln)) { throw "未找到解决方案：$sln（本脚本必须位于 <CJPlug>\05.Build\ 下）" }

# ── 黑名单（枝 13）：运行期残留 + 调试符号 ────────────────────────────────────────────
# ⚠ 两类要区分开（2026-10-07 实测教训）：
#   ①「运行期残留」出现即**中止出包**（*_db/_log 是污染：曾实测把开发机的 station_tasks.db 与
#     StationLogs\log2026*.txt 打进载荷，装到客户机就是拿开发数据覆盖人家数据）；
#     `elsa-signing.key*` 同属此类：它是服务**首次启动自愈生成**的 JWT 签名密钥（方案 §9.7），
#     开发机跑过一次就会落在服务目录里 —— 若随包分发，等于把同一把密钥发给所有客户（可互相伪造 token）。
#   ②「调试符号 *pdb」是 publish 的正常副产物，**静默剔除**即可（PackageService 既有做法也是排除 pdb）——
#     若一并当断言失败，图站三件套 publish 会因 161 个 .pdb 直接打不出包。
$excludeFilePatterns = @('*.db', '*.db-shm', '*.db-wal', '*.log', 'elsa-signing.key*')
$excludeDirNames = @('StationLogs', 'Logs', 'App_Data', '.vs', 'obj', 'bin')
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
    $rcArgs = @($src, $dst, '/E', '/R:1', '/W:1', '/NFL', '/NDL', '/NJH', '/NJS', '/NP', '/XF') + $excludeFilePatterns
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

# ── ⓪ 构建（main 与 desktop 都要用 02.Publish 的 Release 产物；两个组件各跑一次，第二次是增量、秒级） ──
if ($Target -in @('main', 'desktop', 'all') -and -not $SkipBuild) {
    Write-Step "构建解决方案（$Configuration）"
    Invoke-Dotnet 'build solution' @('build', $sln, '-c', $Configuration, '-v:m', '-nologo')
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
    Copy-Tree (Join-Path $publishDir "CJ.Plug.AspireHost.AppHost\$Configuration\net10.0") (Join-Path $mainStaging "CJ.Plug.AspireHost.AppHost\$Configuration\net10.0")
    Copy-Tree (Join-Path $publishDir "Services\$Configuration\net10.0") (Join-Path $mainStaging "Services\$Configuration\net10.0")
    Copy-Tree (Join-Path $publishDir "Services\$Configuration\net10.0-windows") (Join-Path $mainStaging "Services\$Configuration\net10.0-windows")
    Copy-Tree (Join-Path $publishDir 'PlugConfig') (Join-Path $mainStaging 'PlugConfig')

    Remove-ExcludedDirs $mainStaging
    Remove-DebugSymbols $mainStaging

    Assert-Paths $mainStaging @(
        "CJ.Plug.AspireHost.AppHost\$Configuration\net10.0\CJ.Plug.AspireHost.AppHost.dll",
        "CJ.Plug.AspireHost.AppHost\$Configuration\net10.0\aspire\dcp\dcp.exe",
        "CJ.Plug.AspireHost.AppHost\$Configuration\net10.0\aspire\dashboard\Aspire.Dashboard.exe",
        "Services\$Configuration\net10.0\CJ.Plug.ApiServer.dll",
        "Services\$Configuration\net10.0\CJ.Plug.McpServer.dll",
        "Services\$Configuration\net10.0\CJ.Plug.DispatchServer.dll",
        "Services\$Configuration\net10.0-windows\CJ.Plug.ExecuteApp.exe",
        'PlugConfig\UserPlugs.xml'
    )
    Assert-Clean $mainStaging

    Write-Host ("  main\    : {0} MB" -f (Get-TreeSizeMB $mainStaging)) -ForegroundColor Green
}

# ── ② desktop staging（cjplug-desktop：个人形态，仅桌面端） ────────────────────────────
if ($Target -in @('desktop', 'all')) {
    Write-Step "组装 desktop（个人形态：仅 CJ.Plug.Desktop）"

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
    Assert-Clean $stationStaging

    Write-Host ("  station\ : {0} MB" -f (Get-TreeSizeMB $stationStaging)) -ForegroundColor Green
}

Write-Host ''
Write-Host "发布根组装完成：$releaseRoot" -ForegroundColor Green
exit 0
