namespace CJ.Plug.MainPageContent.Shell;

/// <summary>
/// 执行日志抽屉开合桥（2026-10-29《CJPlug 接入 CJCore》P2-2.6）。
/// <para>
/// 抽屉组件必须挂在 <c>MudLayout</c> 直接子级（AppBar 内渲染会被裁剪导致打不开），
/// 而触发按钮挂在 AppBar 内 —— 两者不在同一渲染子树，无法直接互调，
/// 故用一个进程内单例做一次转发。
/// </para>
/// <para>
/// 单例是刻意的：抽屉只有一份（框架布局全局唯一），跨电路共享才不会出现
/// 「A 电路点按钮、B 电路的抽屉响应」。
/// </para>
/// </summary>
public sealed class PlugExecLogService
{
    /// <summary>抽屉开合状态变化通知。</summary>
    public event Action<bool>? StateChanged;

    /// <summary>抽屉当前是否打开（由抽屉组件回写，供按钮切换图标）。</summary>
    public bool IsOpen { get; private set; }

    /// <summary>切换开合。</summary>
    public void Toggle() => SetOpen(!IsOpen);

    /// <summary>直接置位（关闭按钮用）。</summary>
    public void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        StateChanged?.Invoke(open);
    }
}
