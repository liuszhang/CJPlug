using CJCore.Framework.Abstractions;

namespace CJ.Plug.MainPageContent.Shell;

/// <summary>
/// 宿主静态资源贡献模块（2026-10-29《CJPlug 接入 CJCore》P2，替代原 App_All.razor）。
/// <para>
/// 换用框架根组件 <c>FrameworkApp</c> 后，宿主不再有自己的 App.razor，原 App_All 里的
/// head/body 静态资源标签改由本模块经 <see cref="IAppHeadService"/> 贡献，由框架统一渲染。
/// </para>
/// <para>
/// ⚠ 顺序敏感项（不得调换，回归重点见方案 §8.3）：
/// ① BlazorMonaco 三个脚本必须在 head 内，且 loader.js 早于 editor.main.js；
/// ② amis 的 sdk.js 必须在 body 末尾，紧随其后的 amisLoad 内联脚本依赖 amisRequire；
/// ③ noVNC / xterm 的 interop 脚本必须在各自库脚本之后。
/// </para>
/// </summary>
public sealed class PlugHeadModule(IAppHeadService head) : ModuleBase
{
    public override string Name => "插座静态资源";

    public override string ModuleId => "plug-head";

    public override string Group => "宿主扩展";

    /// <summary>资源是根页面可用性的前提：不给关。</summary>
    public override bool IsMandatory => true;

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        // ── head：样式表 ──
        head.AddStylesheet("app.css");
        head.AddHeadHtml("""<link rel="icon" type="image/png" href="plug2.png" />""");
        head.AddStylesheet("https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap");
        head.AddStylesheet("_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css");

        head.AddStylesheet("_content/Z.Blazor.Diagrams/style.min.css");
        head.AddStylesheet("_content/Z.Blazor.Diagrams/default.styles.min.css");

        head.AddStylesheet("_content/CJ.Plug.HomePage/css/prism.css");
        head.AddStylesheet("_content/CJ.Plug.HomePage/css/app.css");
        head.AddStylesheet("_content/CJ.Plug.HomePage/css/test.css");

        head.AddStylesheet("_content/ExcelPlug/hst/handsontable.min.css");
        head.AddStylesheet("_content/ExcelPlug/hst/ht-theme-main.min.css");

        head.AddStylesheet("_content/CJ.Plug.AmisIntegration/BlazorAmis.styles.css");
        head.AddStylesheet("_content/CJ.Plug.AmisIntegration/font-awesome-4.7.0/css/font-awesome.min.css");
        head.AddStylesheet("_content/CJ.Plug.AmisIntegration/sdk/sdk.css");
        head.AddStylesheet("_content/CJ.Plug.AmisIntegration/sdk/helper.css");
        head.AddStylesheet("_content/CJ.Plug.AmisIntegration/sdk/iconfont.css");

        head.AddStylesheet("_content/CJ.Plug.GuacamoleUI/lib/xterm/xterm.css");

        // ── head：Monaco 脚本（必须留在 head 内，且 loader 早于 editor.main）──
        head.AddHeadHtml("""<script src="_content/BlazorMonaco/jsInterop.js"></script>""");
        head.AddHeadHtml("""<script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js"></script>""");
        head.AddHeadHtml("""<script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js"></script>""");

        // ── body 尾部：脚本（MudBlazor / MudExtensions 由框架或本模块补齐）──
        head.AddScript("_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js");
        head.AddScript("_content/Z.Blazor.Diagrams/script.min.js");
        head.AddScript($"_content/Radzen.Blazor/Radzen.Blazor.js?v={typeof(Radzen.Colors).Assembly.GetName().Version}");

        // amis：sdk.js 与依赖它的内联脚本必须相邻且保持此顺序
        head.AddScript("_content/CJ.Plug.AmisIntegration/sdk/sdk.js");
        head.AddBodyHtml("""
<script type="text/javascript">
    amisLoad=(id,json) =>{
        const obj = JSON.parse(json);
        let amis=amisRequire("amis/embed");
        let amisScoped = amis.embed("#"+id, obj);
    };
</script>
""");

        // noVNC（VNC 客户端）
        head.AddScript("_content/CJ.Plug.GuacamoleUI/lib/novnc/novnc.min.js");

        // xterm.js（SSH 终端）
        head.AddScript("_content/CJ.Plug.GuacamoleUI/lib/xterm/xterm.js");
        head.AddScript("_content/CJ.Plug.GuacamoleUI/lib/xterm/xterm-addon-fit.js");

        // 远程桌面 JS 互操作（依赖上面的库）
        head.AddScript("_content/CJ.Plug.GuacamoleUI/noVncInterop.js");
        head.AddScript("_content/CJ.Plug.GuacamoleUI/xtermInterop.js");
        head.AddScript("_content/CJ.Plug.GuacamoleUI/captureInterop.js");

        return ValueTask.CompletedTask;
    }
}
