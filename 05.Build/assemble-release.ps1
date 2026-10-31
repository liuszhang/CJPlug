<#
.SYNOPSIS
    CJPlug 组件载荷组装（《（进行中）CJPlug打包按CJOntology口径重设计方案-20261031》§5.1）。

.DESCRIPTION
    口径对照 CJOntology：**每个组件一棵载荷树，全部由显式 `dotnet publish -o` 产出**。
    本脚本**不再**读共享构建目录 `02.Publish\Services\{Cfg}\{tfm}` —— 那条路已于 2026-10-31 断链
    （宿主改落各自 bin\Release\net10.0，共享目录里只剩旧副本；干净机上连旧副本都没有，
    旧版脚本会在 Assert-Paths 处直接失败）。因此输出目录怎么改都与打包无关。

    产出（均相对 CJPlug 根）：

        -Component cjplug
            05.Release\cjplug\                                  → 组件安装根 = %ProgramFiles%\CJ\CJPlug
              Services\<Cfg>\net10.0\        ← publish CJ.Plug.StationApiServer（先：专属配置由发布侧 target 生成）
              Services\<Cfg>\net10.0\        ← publish CJPlug.Server（后：appsettings.json 由宿主胜出）
              Services\<Cfg>\net10.0-windows\ ← publish CJ.Plug.ExecuteApp
              PlugConfig\                    ← 02.Publish\PlugConfig（源码目录，随包维护）

        -Component cjplug-orchestrator
            05.Release\cjplug-orchestrator\                     → 安装根同上（与 cjplug 共根，运行期契约要求）
              CJCore.AspireHost.AppHost\<Cfg>\net10.0\          ← publish AppHost（含 aspire\dcp、aspire\dashboard）
              CJCore.AppHost.Manager\<Cfg>\net10.0-windows\     ← publish 服务管理壳

        -Component cjplug-desktop
            05.Release\cjplug-desktop\
              CJ.Plug.Desktop\<Cfg>\net10.0-windows\

        -Component station-settingui
            05.Release\station-settingui\                       → 组件安装根 = %ProgramFiles%\CJ\CJPlugStation
              Agent\ 、ApiServer\ 、SettingUI\

    运行期硬契约（不要改树形，改了就运行期错位）：
      · 宿主 cwd = Services\<Cfg>\net10.0；`Data Source=../../main.db` ⇒ 库落 Services\main.db
      · `../../../PlugConfig` ⇒ 插件配置必须在本组件根
      · 执行器由宿主按 hostDir\..\net10.0-windows 找
      · AppHost 按自身目录上溯三级 + Services\<Cfg>\net10.0 推导服务目录；管理壳上溯三级找 AppHost

.NOTES
    退出码：0 = 成功；非 0 = 失败（CJSuite.Pack 的「前置构建」按非 0 中止出包）。
    幂等：每次先清空目标 staging 目录再组装。
    本脚本**不提交 git**、不申请提权；只写 CJPlug\05.Release 与（-DevSync 时）02.Publish\Services。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('cjplug', 'cjplug-orchestrator', 'cjplug-desktop', 'station-settingui')]
    [string]$Component,

    [string]$Configuration = 'Release',

    # 目标 RID：只用于**裁剪 runtimes/**（保留本 RID 与 RID 图中更宽的 win/unix 回退目录）。
    # ⚠ 不把 -r 传给 publish：传了会把 RID 传染给整条工程图，net48 的桥接工程
    #   （DotnetCoreBridgeToDotnetFramework，被 CSharpPlug 引用）会因 RID 与自身 win-x86 默认值不符
    #   直接 NETSDK1047 失败。故发布走 RID-less，落地后再裁掉其它平台的运行期原生库。
    [string]$Runtime = 'win-x64',

    # 版本号：本参数 > 环境变量 CJSUITE_VERSION（打包器 RunPreBuildIfDeclared 注入）> CJSetup\packages.json 的 suite.version
    [string]$Version,

    # 只重组装、不 publish（排障用；要求 05.Release 下已有上次产物）
    [switch]$SkipPublish,

    # 开发期专用：把 cjplug 的 Services 树回灌 02.Publish\Services\{Cfg}\{tfm}，让本地 AppHost 直接可跑。
    # ⚠ 这不是打包路径：打包只认 05.Release\<组件>（清单 sourceDir）。
    [switch]$DevSync
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# ── 控制台输出编码：钉死 UTF-8（2026-10-07 实测，勿删） ────────────────────────────────
# 本脚本常被打包器以「重定向 stdout（进程无控制台）」的方式拉起；此时 PowerShell 会按系统 OEM
# 代码页（本机 936/GBK）写中文，而调用方按 UTF-8 解码 ⇒ 中文全变 U+FFFD。
try { [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false) } catch { }

# ── 路径 ─────────────────────────────────────────────────────────────────────────────
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publishRoot = Join-Path $repoRoot '02.Publish'
$releaseRoot = Join-Path $repoRoot '05.Release'
$setupRoot = Join-Path (Split-Path $repoRoot -Parent) 'CJSetup'

$proj = @{
    Host         = Join-Path $repoRoot 'src\Server\CJPlug.Server\CJPlug.Server.csproj'
    StationApi   = Join-Path $repoRoot 'src\PlugStation\CJ.Plug.StationApiServer\CJ.Plug.StationApiServer.csproj'
    ExecuteApp   = Join-Path $repoRoot 'src\modules\PlugExecuteModule\CJ.Plug.ExecuteApp\CJ.Plug.ExecuteApp.csproj'
    Desktop      = Join-Path $repoRoot 'src\WebHost\CJ.Plug.Desktop\CJ.Plug.Desktop.csproj'
    StationAgent = Join-Path $repoRoot 'src\PlugStation\CJ.Plug.StationAgent\CJ.Plug.StationAgent.csproj'
    StationUi    = Join-Path $repoRoot 'src\PlugStation\StationSettingUI\StationSettingUI.csproj'
}
# 跨仓（CJCore）：编排壳与管理壳的源码已迁出 CJPlug（2026-10-29/30），路径按兄弟目录相对定位。
$proj.AppHost = Join-Path $repoRoot '..\CJCore\src\Apps\CJCore.AspireHost.AppHost\CJCore.AspireHost.AppHost.csproj'
$proj.Manager = Join-Path $repoRoot '..\CJCore\src\Apps\CJCore.AppHost.Manager\CJCore.AppHost.Manager.csproj'

$plugConfigSrc = Join-Path $publishRoot 'PlugConfig'

# ── 运行期残留 / 调试符号黑名单（沿用 2026-10-07 起的既有口径，勿松） ────────────────────
$excludeFilePatterns = @(
    '*.db', '*.db-shm', '*.db-wal', '*.log', 'elsa-signing.key*', 'window-state.json',
    'apphost.user.json',   # 编排壳的用户覆盖配置：含开发机绝对路径
    'services.user.json'   # 管理壳的服务清单覆盖：同上（2026-10-31 起纳入）
)
$excludeDirNames = @('StationLogs', 'Logs', 'App_Data', '.vs', 'obj', 'bin',
    'CJ.Plug.Desktop.exe.WebView2', 'CJCore.AppHost.Manager.exe.WebView2', 'DshRuntime')
$stripFilePatterns = @('*.pdb')

function Write-Step([string]$msg) {
    Write-Host ''
    Write-Host "=== $msg ===" -ForegroundColor Cyan
}

function Reset-Staging([string]$path) {
    # 防误删：只允许清理位于 <CJPlug>\05.Release\ 之下的目录，且必须解析为绝对路径
    $full = [System.IO.Path]::GetFullPath($path)
    $rootFull = [System.IO.Path]::GetFullPath($releaseRoot)
    if (-not $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "拒绝清理非发布根下的目录：$full"
    }
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $full | Out-Null
}

function Assert-Proj([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { throw "未找到工程文件：$path" }
}

function Invoke-Publish([string]$desc, [string]$csproj, [string]$outDir, [string[]]$extraArgs = @()) {
    Assert-Proj $csproj
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    # RID-less 的**框架依赖发布**（D3 = 全线框架依赖），理由见参数块 $Runtime：
    #   · 不带 -r：避免 RID 传染给整条工程图（net48 桥接工程会 NETSDK1047）；
    #   · DisableBuildCompression=true：RID-less 发布时 SDK 的 ApplyCompressionNegotiation 会在
    #     CJ.Plug.ApiServer（Sdk.Web 类库）上抛 "Endpoints not found for compressed asset"（MSB4018，
    #     纯 bin 侧压缩问题）。关掉 build 侧压缩不影响载荷 —— publish 侧仍按 PublishCompressionFormats
    #     产出 .gz/.br（实测 1000+ 个）。
    $args = @('publish', $csproj, '-c', $Configuration, '--self-contained=false',
        '-p:DisableBuildCompression=true', '--nologo', '-o', $outDir)
    if (-not [string]::IsNullOrWhiteSpace($Version)) { $args += "-p:Version=$Version" }
    if ($extraArgs.Count -gt 0) { $args += $extraArgs }
    Write-Host "[$desc] dotnet $($args -join ' ')" -ForegroundColor DarkGray
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "$desc 失败（exit=$LASTEXITCODE）：dotnet $($args -join ' ')" }
}

function Remove-ForeignRuntimeAssets([string]$stagingRoot) {
    # 裁剪 runtimes/：只保留本 RID、RID 图中的更宽目录（win / unix）与 any。
    # 依据：载荷只面向 win-x64 部署；不裁会把 win-arm64/win-x86/linux*/osx*/android*/ios* 全带上
    # （实测宿主载荷 runtimes 412.9MB，其中 win-arm64 104.3MB + win-x86 110.1MB 纯浪费）。
    $keep = @('win', $Runtime, 'unix', 'any')
    $removed = 0; $removedMb = 0.0
    Get-ChildItem -LiteralPath $stagingRoot -Directory -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { $_.Parent -and $_.Parent.Name -eq 'runtimes' } |
        Where-Object { $keep -notcontains $_.Name } |
        ForEach-Object {
            $mb = (Get-ChildItem -LiteralPath $_.FullName -Recurse -File -ErrorAction SilentlyContinue |
                    Measure-Object -Property Length -Sum).Sum
            $removedMb += ($mb / 1MB)
            Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
            $removed++
        }
    if ($removed -gt 0) {
        Write-Host ("  [strip] 已裁剪非 {0} 的 runtimes 目录 {1} 个（约 {2} MB）" -f $Runtime, $removed, [math]::Round($removedMb, 1))
    }
}

function Copy-Tree([string]$src, [string]$dst) {
    if (-not (Test-Path -LiteralPath $src)) { throw "源目录不存在：$src" }
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    # robocopy 退出码 0..7 均为成功（位标志），>=8 才是失败；黑名单走源头排除（省一次整拷）。
    $rcArgs = @($src, $dst, '/E', '/R:1', '/W:1', '/NFL', '/NDL', '/NJH', '/NJS', '/NP',
        '/XD') + $excludeDirNames + @('/XF') + $excludeFilePatterns
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
        if (-not (Test-Path -LiteralPath $full)) { $missing += $rel }
    }
    if ($missing.Count -gt 0) {
        throw "组装后断言失败（关键入口缺失）：`n  - $(($missing -join "`n  - "))`n（staging: $stagingRoot）"
    }
}

function Assert-Absent([string]$stagingRoot, [string[]]$relativePaths, [string]$reason) {
    $found = @()
    foreach ($rel in $relativePaths) {
        $full = Join-Path $stagingRoot $rel
        if (Test-Path -LiteralPath $full) { $found += $rel }
    }
    if ($found.Count -gt 0) {
        throw "组装后断言失败（$reason，不应出现 $($found.Count) 项）：`n  - $(($found -join "`n  - "))`n（staging: $stagingRoot）"
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

function Get-Sha256([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return '' }
    return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-TreeSizeMB([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return 0 }
    $sum = (Get-ChildItem -LiteralPath $path -File -Recurse -Force -ErrorAction SilentlyContinue |
            Measure-Object -Property Length -Sum).Sum
    return [math]::Round($sum / 1MB, 1)
}

function Resolve-Version {
    # 优先级：-Version > CJSUITE_VERSION（打包器注入）> CJSetup\packages.json 的 suite.version
    if (-not [string]::IsNullOrWhiteSpace($Version)) { return $Version.Trim() }
    $fromEnv = [string]$env:CJSUITE_VERSION
    if (-not [string]::IsNullOrWhiteSpace($fromEnv)) { return $fromEnv.Trim() }
    $manifest = Join-Path $setupRoot 'packages.json'
    if (Test-Path -LiteralPath $manifest) {
        try {
            $json = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($json.suite -and -not [string]::IsNullOrWhiteSpace($json.suite.version)) { return ([string]$json.suite.version).Trim() }
        } catch { }
    }
    return ''
}

$Version = Resolve-Version

# ══════════════════════════════════════════════════════════════════════════════════════
Write-Host "CJPlug 组件载荷组装" -ForegroundColor Green
Write-Host "  CJPlug 根 : $repoRoot"
Write-Host "  配置      : $Configuration"
Write-Host "  组件      : $Component"
Write-Host "  版本      : $(if ($Version) { $Version } else { '（未指定，publish 不带 -p:Version）' })"

switch ($Component) {

    # ── ① 主服务端：宿主 + 本机工位机 + 独立执行器 + PlugConfig（一棵树，零重复） ─────────
    'cjplug' {
        $staging = Join-Path $releaseRoot 'cjplug'
        $services = Join-Path $staging "Services\$Configuration\net10.0"
        $servicesWin = Join-Path $staging "Services\$Configuration\net10.0-windows"

        if (-not $SkipPublish) {
            Write-Step '组装 cjplug（主服务端）：publish 工位机 → 宿主 → 执行器'
            Reset-Staging $staging | Out-Null
            # 顺序是**契约**：两个可执行程序都有同名 appsettings.json。工位机先发（连同发布侧 target
            # 生成的 appsettings.CJ.Plug.StationApiServer.json），宿主最后发，确保落地的根配置是宿主那份。
            Invoke-Publish 'publish stationapi' $proj.StationApi $services
            # ⚠ 宿主必须关掉 SDK 的预压缩（`EnableDefaultCompressionFormats=false`），理由是一次实测到的启动硬故障：
            #   宿主把 Blazor 运行时资产（`wwwroot/_framework/blazor*.js`，见该目录 README）当普通静态资产参与
            #   publish 压缩协商，与框架自带的那份 `.br/.gz` 路由撞车 ⇒ 启动即
            #   `InvalidOperationException: The static asset '_framework/blazor.server.js.br' is already mapped to
            #   _framework/blazor.server.<hash>.js.br`（IHost 直接起不来，不是"有外观无事件"而是"根本起不来"）。
            #   关掉预压缩后：`_framework/blazor.web.js` 实测 200 / 200645B / text/javascript，首页 200。
            #   代价：载荷内不再有 `.gz/.br` 预压缩副本（改由响应压缩/Kestrel 实时压缩），体积与传输特性略变。
            Invoke-Publish 'publish host'       $proj.Host       $services -extraArgs @('-p:EnableDefaultCompressionFormats=false')
            Invoke-Publish 'publish executeapp' $proj.ExecuteApp $servicesWin

            Write-Step '组装 cjplug：拷贝 PlugConfig'
            Copy-Tree $plugConfigSrc (Join-Path $staging 'PlugConfig')
        }
        else {
            Write-Host '  （-SkipPublish：跳过 publish，仅重组装/断言）' -ForegroundColor Yellow
        }

        Remove-ExcludedDirs $staging
        Remove-DebugSymbols $staging
        Remove-ForeignRuntimeAssets $staging

        Assert-Paths $staging @(
            "Services\$Configuration\net10.0\CJPlug.Server.dll",
            "Services\$Configuration\net10.0\CJPlug.Server.exe",
            "Services\$Configuration\net10.0\appsettings.json",
            # Blazor 运行时资产：由 BlazorFrameworkAssetsEndpoints 从物理 wwwroot/_framework 吐出。
            # SDK 不产出它们（清单里 _framework 条目数为 0），必须靠源码随包（见该目录 README）；
            # 缺了下场就是"整站有外观、无事件"，所以钉成硬断言。
            "Services\$Configuration\net10.0\wwwroot\_framework\blazor.web.js",
            "Services\$Configuration\net10.0\wwwroot\_framework\blazor.server.js",
            "Services\$Configuration\net10.0\CJ.Plug.StationApiServer.dll",
            "Services\$Configuration\net10.0\appsettings.CJ.Plug.StationApiServer.json",
            "Services\$Configuration\net10.0-windows\CJ.Plug.ExecuteApp.exe",
            'PlugConfig\UserPlugs.xml'
        )
        # 负向断言：回退形态 / UI 组件 exe 不得回潮（P9 之后它们只是类库）。
        # ⚠ 工位机 exe 是**本组件的合法内容**（AppHost 用 dotnet 拉起它的 dll，exe 留着无害且便于排障），
        #   故不再像旧脚本那样从载荷剔除。
        Assert-Absent $staging @(
            "Services\$Configuration\net10.0\CJ.Plug.ApiServer.exe",
            "Services\$Configuration\net10.0\CJ.Plug.DispatchServer.exe",
            "Services\$Configuration\net10.0\CJ.Plug.McpServer.exe",
            "Services\$Configuration\net10.0\CJ.Plug.HostWebServer.exe",
            "Services\$Configuration\net10.0\CJ.Plug.AIChat.exe"
        ) '回退形态 / UI 组件 exe 应已全部下线'
        # 配置归一：类库化项目不应再有专属配置
        Assert-Absent $staging @(
            "Services\$Configuration\net10.0\appsettings.CJ.Plug.ApiServer.json",
            "Services\$Configuration\net10.0\appsettings.CJ.Plug.DispatchServer.json",
            "Services\$Configuration\net10.0\appsettings.CJ.Plug.McpServer.json",
            "Services\$Configuration\net10.0\appsettings.CJ.Plug.HostWebServer.json",
            "Services\$Configuration\net10.0\appsettings.CJPlug.Server.json"
        ) '配置归一后不应再出现类库项目的专属配置'
        Assert-Clean $staging

        # 关键断言：落地的根配置必须是宿主的（防"后发的工位机配置盖掉主配置"这类静默故障）
        $srcCfg = Join-Path $repoRoot 'src\Server\CJPlug.Server\appsettings.json'
        $dstCfg = Join-Path $services 'appsettings.json'
        $srcSha = Get-Sha256 $srcCfg
        $dstSha = Get-Sha256 $dstCfg
        if ($srcSha -and $dstSha -and $srcSha -ne $dstSha) {
            throw "组装后断言失败（appsettings.json 不是宿主那份：src=$srcSha / payload=$dstSha）—— 检查 publish 顺序与发布侧 target。"
        }

        if ($DevSync) {
            Write-Step 'DevSync：把 Services 树回灌 02.Publish\Services（仅开发期让本地 AppHost 直接跑）'
            $devServices = Join-Path $publishRoot "Services\$Configuration\net10.0"
            $devServicesWin = Join-Path $publishRoot "Services\$Configuration\net10.0-windows"
            Copy-Tree $services $devServices
            Copy-Tree $servicesWin $devServicesWin
            Write-Host "  [DevSync] $devServices" -ForegroundColor Green
        }

        Write-Host ("  cjplug\   : {0} MB" -f (Get-TreeSizeMB $staging)) -ForegroundColor Green
    }

    # ── ② 编排台：AppHost（含 DCP/Dashboard）+ 管理壳 ─────────────────────────────────
    'cjplug-orchestrator' {
        $staging = Join-Path $releaseRoot 'cjplug-orchestrator'
        $appHostOut = Join-Path $staging "CJCore.AspireHost.AppHost\$Configuration\net10.0"
        $managerOut = Join-Path $staging "CJCore.AppHost.Manager\$Configuration\net10.0-windows"

        if (-not $SkipPublish) {
            Write-Step '组装 cjplug-orchestrator：publish AppHost → 管理壳'
            Reset-Staging $staging | Out-Null
            # ⚠ Aspire AppHost 的 SDK 把 `IsPublishable` 默认设为 false（它是"运行器"，官方发布口径是
            #   `aspire publish`，不是 `dotnet publish`）⇒ 不加下面这个开关，publish 只跑我们补的
            #   aspire 拷贝目标、**不产 App 本体**（实测输出目录里只有 aspire\ 一个子目录）。
            #   本组件要的是"可整树拷贝的运行目录"，故显式打开；deps.json/runtimeconfig 由普通
            #   Exe 发布管线生成，与手动 `dotnet` 拉起无关（AppHost 自己的 exe 也一并产出）。
            Invoke-Publish 'publish apphost' $proj.AppHost $appHostOut -extraArgs @('-p:IsPublishable=true')
            Invoke-Publish 'publish manager' $proj.Manager $managerOut
        }
        else {
            Write-Host '  （-SkipPublish：跳过 publish，仅重组装/断言）' -ForegroundColor Yellow
        }

        Remove-ExcludedDirs $staging
        Remove-DebugSymbols $staging
        Remove-ForeignRuntimeAssets $staging

        Assert-Paths $staging @(
            "CJCore.AspireHost.AppHost\$Configuration\net10.0\CJCore.AspireHost.AppHost.dll",
            "CJCore.AspireHost.AppHost\$Configuration\net10.0\CJCore.AspireHost.AppHost.exe",
            "CJCore.AspireHost.AppHost\$Configuration\net10.0\aspire\dcp\dcp.exe",
            "CJCore.AspireHost.AppHost\$Configuration\net10.0\aspire\dashboard\Aspire.Dashboard.exe",
            "CJCore.AppHost.Manager\$Configuration\net10.0-windows\CJCore.AppHost.Manager.exe"
        )
        # 编排壳出厂配置：含 Resources[] 与端口契约，缺了 AppHost 起来但"没有服务"
        Assert-Paths $staging @("CJCore.AspireHost.AppHost\$Configuration\net10.0\appsettings.json")
        Assert-Clean $staging

        Write-Host ("  cjplug-orchestrator\ : {0} MB" -f (Get-TreeSizeMB $staging)) -ForegroundColor Green
    }

    # ── ③ 桌面端 ──────────────────────────────────────────────────────────────────────
    'cjplug-desktop' {
        $staging = Join-Path $releaseRoot 'cjplug-desktop'
        $desktopOut = Join-Path $staging "CJ.Plug.Desktop\$Configuration\net10.0-windows"

        if (-not $SkipPublish) {
            Write-Step '组装 cjplug-desktop：publish CJ.Plug.Desktop'
            Reset-Staging $staging | Out-Null
            Invoke-Publish 'publish desktop' $proj.Desktop $desktopOut
        }
        else {
            Write-Host '  （-SkipPublish：跳过 publish，仅重组装/断言）' -ForegroundColor Yellow
        }

        Remove-ExcludedDirs $staging
        Remove-DebugSymbols $staging
        Remove-ForeignRuntimeAssets $staging

        Assert-Paths $staging @("CJ.Plug.Desktop\$Configuration\net10.0-windows\CJ.Plug.Desktop.exe")
        Assert-Clean $staging

        Write-Host ("  cjplug-desktop\ : {0} MB" -f (Get-TreeSizeMB $staging)) -ForegroundColor Green
    }

    # ── ④ 图站：Agent + ApiServer + SettingUI（三件套各自 publish；与现网结构一致） ──────
    'station-settingui' {
        $staging = Join-Path $releaseRoot 'station-settingui'

        if (-not $SkipPublish) {
            Write-Step '组装 station-settingui：publish Agent → ApiServer → SettingUI'
            Reset-Staging $staging | Out-Null
            Invoke-Publish 'publish station agent' $proj.StationAgent (Join-Path $staging 'Agent')
            Invoke-Publish 'publish station api'   $proj.StationApi   (Join-Path $staging 'ApiServer')
            Invoke-Publish 'publish station ui'    $proj.StationUi    (Join-Path $staging 'SettingUI')
        }
        else {
            Write-Host '  （-SkipPublish：跳过 publish，仅重组装/断言）' -ForegroundColor Yellow
        }

        Remove-ExcludedDirs $staging
        Remove-DebugSymbols $staging
        Remove-ForeignRuntimeAssets $staging

        Assert-Paths $staging @(
            'Agent\CJ.Plug.StationAgent.exe',
            'ApiServer\CJ.Plug.StationApiServer.exe',
            'SettingUI\StationSettingUI.exe'
        )
        # API 侧的专属配置由发布侧 target 生成（Agent/SettingUI 无同级同名配置冲突）
        Assert-Paths $staging @('ApiServer\appsettings.CJ.Plug.StationApiServer.json')
        Assert-Absent $staging @('ApiServer\CJ.Plug.AIChat.exe') '图站载荷不得混入库组件 exe（CJ.Plug.AIChat 已类库化）'
        Assert-Clean $staging

        Write-Host ("  station-settingui\ : {0} MB" -f (Get-TreeSizeMB $staging)) -ForegroundColor Green
    }
}

Write-Host ''
Write-Host "组件载荷组装完成：$(Join-Path $releaseRoot $Component)" -ForegroundColor Green
exit 0
