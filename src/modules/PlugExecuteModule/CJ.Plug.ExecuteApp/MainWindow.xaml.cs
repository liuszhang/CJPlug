using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace CJ.Plug.ExecuteApp;

/// <summary>
/// 插头执行独立窗口 —— 通过 WebView2 加载 PlugExecuteStandalone 页面。
///
/// <para><b>用法</b>：<c>CJ.Plug.ExecuteApp.exe [baseUrl] &lt;PlugDefinitionId&gt; [JobDefinitionId] [--ticket=code]</c></para>
/// <list type="bullet">
/// <item>例 1（主入口）：<c>CJ.Plug.ExecuteApp.exe "http://host:5066" P100 --ticket=AbC…</c></item>
/// <item>例 2（手工调试）：<c>CJ.Plug.ExecuteApp.exe http://localhost:5066 P100</c> —— 无票据，落到登录页手工登录</item>
/// </list>
///
/// <para><b>为什么需要 --ticket（P5.9 / D-P5-7）</b>：WebView2 是**空白 cookie 容器**，不像用户浏览器带着
/// 登录态，直接 Navigate 到 <c>/process/{id}/execute</c> 必然 302 登录页。服务端用 CJCore 的一次性换票码
/// （<c>CJCoreEmbedTicketStore</c>，60 秒 / 单次 / 192 位随机）把当前用户的 AT 换成一个短码，本进程先导航到
/// <c>/{prefix}/embed-cookie?code=…</c>——该端点验码后签发 <c>.ThreeRole.Auth</c> + <c>.ThreeRole.Token</c>
/// 两枚 Cookie 并 302 到目标页，页面即带完整身份渲染。</para>
///
/// <para><b>为什么不在本进程手工注入 Cookie</b>：<c>.ThreeRole.Auth</c> 是 ASP.NET Core **加密票据**，
/// 客户端无从构造合法值；<c>CookieManager.CreateCookie</c> 也设不了 HttpOnly/SameSite/过期语义。
/// 换票码让服务端按 <c>HttpContext</c> 正确判定 Secure 等属性，是唯一正解。</para>
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>CJCore 认证基座的端点组前缀（= CJCoreAuthOptions.GroupPrefix）。</summary>
    private const string AuthEndpointPrefix = "api/three-role/account";

    private string? _baseUrl;
    private string? _plugDefinitionId;
    private string? _jobDefinitionId;

    /// <summary>一次性换票码（服务端签发，60 秒内单次有效）。缺失时降级为直连执行页（→ 登录页）。</summary>
    private string? _ticket;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ParseCommandLineArgs();

        if (string.IsNullOrEmpty(_plugDefinitionId))
        {
            MessageBox.Show(
                "缺少必要参数 PlugDefinitionId。\n" +
                "用法: CJ.Plug.ExecuteApp.exe [baseUrl] <PlugDefinitionId> [JobDefinitionId] [--ticket=code]",
                "参数错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        await InitializeWebViewAsync();
    }

    private void ParseCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();

        // ⚠ 必须先摘具名参数（--xxx=yyy）再做位置解析：否则 --ticket=xxx 会被当成 PlugDefinitionId 或
        //    JobDefinitionId 吞掉，表现为「窗口打开的是登录页」而看不出参数错在哪。
        var userArgs = new List<string>();
        foreach (var raw in args.Skip(1))
        {
            if (raw.StartsWith("--ticket=", StringComparison.OrdinalIgnoreCase))
            {
                _ticket = raw["--ticket=".Length..];
            }
            else if (raw.StartsWith("--", StringComparison.Ordinal))
            {
                // 未知具名参数：忽略，不参与位置解析
            }
            else
            {
                userArgs.Add(raw);
            }
        }

        // 尝试自动检测 baseUrl（如果第一个位置参数看起来像 URL）
        if (userArgs.Count >= 1 && IsUrl(userArgs[0]))
        {
            _baseUrl = userArgs[0].TrimEnd('/');
            userArgs.RemoveAt(0);
        }

        if (userArgs.Count >= 1)
            _plugDefinitionId = userArgs[0];

        if (userArgs.Count >= 2)
            _jobDefinitionId = userArgs[1];
    }

    private static bool IsUrl(string input)
        => input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || input.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private async Task InitializeWebViewAsync()
    {
        var env = await CoreWebView2Environment.CreateAsync();
        await WebView.EnsureCoreWebView2Async(env);

        // 禁止 WebView2 右键菜单和开发者工具
        WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = Debugger.IsAttached;

        // 目标页，格式对应 PlugExecuteStandalone.razor 的路由:
        // /process/{PlugDefinitionId}/execute/{JobDefinitionId?}
        var targetPath = string.IsNullOrEmpty(_jobDefinitionId)
            ? $"/process/{_plugDefinitionId}/execute"
            : $"/process/{_plugDefinitionId}/execute/{_jobDefinitionId}";

        Title = $"插头执行 - {_plugDefinitionId}";

        // 有票据 → 先经过认证基座落地 Cookie，再由它 302 到目标页（一次导航，用户无感）。
        // 无票据 → 直连目标页（[Authorize] 会 302 到登录页，人工登录后可继续用）。
        var startUrl = string.IsNullOrEmpty(_ticket)
            ? $"{_baseUrl}{targetPath}"
            : $"{_baseUrl}/{AuthEndpointPrefix}/embed-cookie" +
              $"?code={Uri.EscapeDataString(_ticket)}" +
              $"&returnUrl={Uri.EscapeDataString(targetPath)}";

        WebView.CoreWebView2.Navigate(startUrl);
    }
}
