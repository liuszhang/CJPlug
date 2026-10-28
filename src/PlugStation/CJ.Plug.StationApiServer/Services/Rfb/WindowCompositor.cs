using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace CJ.Plug.StationApiServer.Services.Rfb
{
    /// <summary>
    /// 窗口合成目标：由 WindowBindRegistry 提供（PID 主 / 进程名辅）。
    /// </summary>
    public record WindowTarget(int? ProcessId, string? ProcessName);

    /// <summary>
    /// 单个纳入合成的窗口。
    /// </summary>
    public class WindowEntry
    {
        public required IntPtr Handle { get; init; }
        public string Title { get; set; } = "";
        public int ProcessId { get; set; }
        public Rectangle Rect { get; set; }
    }

    /// <summary>
    /// L3 多窗口合成器（CJ.Plug 单窗口 RFB VNC 帧源）。
    /// 枚举目标进程树全部可见顶层窗口 + owned 弹窗 + 跨进程模态链（文件对话框），
    /// 合并包围盒为合成画布，逐窗口 PrintWindow 渲染，输出 32bpp BGRX 帧缓冲 + 脏矩形。
    /// </summary>
    public class WindowCompositor : IDisposable
    {
        #region Win32 P/Invoke

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
        }

        private const uint TH32CS_SNAPPROCESS = 0x00000002;
        private const uint GW_OWNER = 4;
        private const uint PW_RENDERFULLCONTENT = 0x00000002;
        private const uint SRCCOPY = 0x00CC0020;
        private const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr ho);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        #endregion

        private readonly object _lock = new();
        private WindowTarget? _target;
        private HashSet<int> _targetPids = new();
        private List<WindowEntry> _windows = new();
        private Rectangle _bounds;
        private byte[]? _frame;
        private byte[]? _lastFrame;
        private int _stride;
        private bool _frameDirty;      // 窗口集合/位置变化 → 全帧脏
        private readonly List<Rectangle> _dirtyRects = new();
        private bool _diagWindowListPrinted;
        private DateTime _lastDiagTime = DateTime.MinValue;

        public bool HasTarget { get { lock (_lock) return _target != null; } }

        public int Width { get { lock (_lock) return _width; } }
        public int Height { get { lock (_lock) return _height; } }
        public int Stride { get { lock (_lock) return _stride; } }
        private int _width, _height;

        /// <summary>当前合成画布帧（BGRX 32bpp）。仅读。</summary>
        public byte[]? Frame { get { lock (_lock) return _frame; } }

        /// <summary>本帧脏矩形（画布坐标）。</summary>
        public IReadOnlyList<Rectangle> DirtyRects { get { lock (_lock) return _dirtyRects.ToList(); } }

        /// <summary>设置/更新绑定目标（PID 主，进程名辅）。</summary>
        public void SetTarget(WindowTarget? target)
        {
            lock (_lock)
            {
                _target = target;
                _targetPids = ResolveTargetPids(target);
                _windows.Clear();
                _bounds = Rectangle.Empty;
                _frameDirty = true;
            }
        }

        /// <summary>目标是否仍然存活（存在窗口）。</summary>
        public bool IsTargetAlive()
        {
            lock (_lock)
            {
                if (_target == null) return false;
                return _windows.Count > 0 || EnumerateTargetWindows().Count > 0;
            }
        }

        /// <summary>
        /// 仅枚举窗口并更新画布尺寸（不渲染、不消费首帧）。
        /// 供 ServerInit 前获取画面尺寸使用——若直接 CaptureFrame 会渲染并交换帧，
        /// 导致帧循环首次 CaptureFrame 检测不到全帧脏、永不发帧。
        /// </summary>
        public void RefreshSize()
        {
            lock (_lock)
            {
                if (_target == null)
                {
                    Console.WriteLine($"[RfbDiag] RefreshSize: 无目标");
                    return;
                }
                try
                {
                    var windows = EnumerateTargetWindows();
                    var b = MergeBounds(windows);
                    Console.WriteLine($"[RfbDiag] RefreshSize: 窗口数={windows.Count} 合并边界={b}");
                    if (windows.Count == 0 || b.Width <= 0 || b.Height <= 0) return;
                    _width = b.Width;
                    _height = b.Height;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RfbDiag] RefreshSize 异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 采集一帧：枚举窗口 → 渲染画布 → 计算脏矩形。
        /// 返回 true 表示有可发送画面（画布非空）。
        /// </summary>
        public bool CaptureFrame()
        {
            lock (_lock)
            {
                if (_target == null)
                {
                    _dirtyRects.Clear();
                    return false;
                }

                try
                {
                    var windows = EnumerateTargetWindows();

                    // 全帧重算条件：窗口集合为空 / 集合或包围盒变化
                    var newBounds = MergeBounds(windows);
                    bool structureChanged = _frameDirty
                        || _windows.Count != windows.Count
                        || newBounds != _bounds;

                    if (windows.Count == 0 || newBounds.Width <= 0 || newBounds.Height <= 0)
                    {
                        if (DateTime.Now - _lastDiagTime > TimeSpan.FromSeconds(5))
                        {
                            _lastDiagTime = DateTime.Now;
                            Console.WriteLine($"[RfbDiag] CaptureFrame: 无有效窗口 数={windows.Count} 边界={newBounds}");
                        }
                        _dirtyRects.Clear();
                        _windows = windows;
                        _bounds = newBounds;
                        return false;
                    }

                    _windows = windows;
                    _bounds = newBounds;

                    int w = newBounds.Width, h = newBounds.Height;
                    int stride = w * 4;
                    if (_frame == null || _frame.Length != stride * h)
                    {
                        _frame = new byte[stride * h];
                        _lastFrame = new byte[stride * h];
                        structureChanged = true;
                    }
                    _stride = stride;
                    _width = w;
                    _height = h;

                    // 渲染新帧到 _frame
                    RenderAllWindows(windows, newBounds, _frame, stride);

                    _dirtyRects.Clear();
                    if (structureChanged)
                    {
                        // 结构变化 → 全帧脏
                        _dirtyRects.Add(new Rectangle(0, 0, w, h));
                    }
                    else
                    {
                        ComputeDirtyRects(_frame, _lastFrame!, stride, w, h, _dirtyRects);
                    }

                    // 交换帧
                    (_frame, _lastFrame) = (_lastFrame, _frame);
                    _frameDirty = false;
                    if (DateTime.Now - _lastDiagTime > TimeSpan.FromSeconds(5))
                    {
                        _lastDiagTime = DateTime.Now;
                        Console.WriteLine($"[RfbDiag] CaptureFrame: 渲染完成 {w}x{h} 脏矩形={_dirtyRects.Count}");
                    }
                    return _dirtyRects.Count > 0;
                }
                catch (Exception ex)
                {
                    // 渲染异常不致命：保留旧状态，下次重试
                    Console.WriteLine($"[RfbDiag] CaptureFrame 异常: {ex.Message}");
                    Log.Warning(ex, "CaptureFrame 渲染异常，跳过本帧");
                    _dirtyRects.Clear();
                    return false;
                }
            }
        }

        /// <summary>
        /// 画布坐标 → 屏幕坐标（输入注入用）。返回 null 表示不在任何窗口内。
        /// </summary>
        public (int X, int Y)? MapToScreen(int canvasX, int canvasY)
        {
            lock (_lock)
            {
                if (_bounds.IsEmpty) return null;
                int sx = canvasX + _bounds.Left;
                int sy = canvasY + _bounds.Top;
                foreach (var win in _windows)
                {
                    if (win.Rect.Contains(sx, sy))
                        return (sx, sy);
                }
                // 未命中具体窗口也按画布偏移返回（命中画布空白区域时交给系统）
                return (sx, sy);
            }
        }

        /// <summary>当前合成画布原点（屏幕坐标），输入映射/调试用。</summary>
        public Rectangle Bounds { get { lock (_lock) return _bounds; } }

        #region 窗口枚举（L3）

        /// <summary>
        /// 解析目标 PID 集合：显式 PID + 其进程树后代；进程名兜底（无 PID 时）。
        /// </summary>
        private static HashSet<int> ResolveTargetPids(WindowTarget? target)
        {
            var pids = new HashSet<int>();
            if (target == null) return pids;

            if (target.ProcessId.HasValue && target.ProcessId.Value > 0)
            {
                pids.Add(target.ProcessId.Value);
                foreach (var child in GetDescendantPids(target.ProcessId.Value))
                    pids.Add(child);
            }

            if (pids.Count == 0 && !string.IsNullOrEmpty(target.ProcessName))
            {
                foreach (var proc in Process.GetProcessesByName(target.ProcessName))
                    pids.Add(proc.Id);
            }

            return pids;
        }

        /// <summary>获取指定进程的全部子孙进程 PID（ToolHelp32 进程快照）。</summary>
        private static List<int> GetDescendantPids(int rootPid)
        {
            var result = new List<int>();
            var parentMap = new Dictionary<int, List<int>>();
            try
            {
                IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
                if (snapshot == IntPtr.Zero || snapshot == (IntPtr)INVALID_HANDLE_VALUE)
                    return result;
                try
                {
                    var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
                    if (Process32First(snapshot, ref entry))
                    {
                        do
                        {
                            if (!parentMap.TryGetValue((int)entry.th32ParentProcessID, out var list))
                            {
                                list = new List<int>();
                                parentMap[(int)entry.th32ParentProcessID] = list;
                            }
                            list.Add((int)entry.th32ProcessID);
                        } while (Process32Next(snapshot, ref entry));
                    }
                }
                finally
                {
                    // 快照句柄需要 CloseHandle；此处使用 kernel32.CloseHandle
                    CloseHandle(snapshot);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "枚举进程树失败: {Pid}", rootPid);
            }

            // BFS 收集子孙
            var queue = new Queue<int>();
            queue.Enqueue(rootPid);
            while (queue.Count > 0)
            {
                var pid = queue.Dequeue();
                if (parentMap.TryGetValue(pid, out var children))
                {
                    foreach (var child in children)
                    {
                        result.Add(child);
                        queue.Enqueue(child);
                    }
                }
            }
            return result;
        }

        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// 枚举目标合成窗口集合（L3）：
        /// 1) 目标 PID 集合内所有可见顶层窗口（含最小化）；
        /// 2) owned 链属于目标集合的窗口；
        /// 3) 跨进程模态链：前台窗口的 owner 链命中目标集合（覆盖文件对话框等外部进程窗口）。
        /// </summary>
        private List<WindowEntry> EnumerateTargetWindows()
        {
            var result = new List<WindowEntry>();
            if (_targetPids.Count == 0)
            {
                // 进程名兜底已解析进 _targetPids；若仍为空则按进程名直接枚举
                _targetPids = ResolveTargetPids(_target);
                if (_targetPids.Count == 0) return result;
            }

            var allTopWindows = new List<(IntPtr Hwnd, int Pid, string Title, Rectangle Rect)>();
            EnumWindows((hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd))
                    return true;

                // 过滤最小化窗口：GetWindowRect 会返回 -21333/-32000 伪坐标，污染画布合并
                if (IsIconic(hwnd))
                    return true;

                if (!GetWindowRect(hwnd, out var rect) || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
                    return true;

                GetWindowThreadProcessId(hwnd, out var pid);
                int len = GetWindowTextLength(hwnd);
                var title = "";
                if (len > 0)
                {
                    var sb = new StringBuilder(len + 1);
                    GetWindowText(hwnd, sb, sb.Capacity);
                    title = sb.ToString();
                }
                allTopWindows.Add((hwnd, (int)pid, title, new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top)));
                return true;
            }, IntPtr.Zero);

            if (allTopWindows.Count == 0)
            {
                Console.WriteLine($"[RfbDiag] EnumWindows 可见窗口总数=0！_targetPids=[{string.Join(",", _targetPids)}]");
            }
            else if (!_diagWindowListPrinted)
            {
                _diagWindowListPrinted = true;
                Console.WriteLine($"[RfbDiag] EnumWindows 可见窗口={allTopWindows.Count} 目标PIDs=[{string.Join(",", _targetPids)}] 匹配={allTopWindows.Count(w => _targetPids.Contains(w.Pid))}");
                foreach (var w in allTopWindows.Take(40))
                {
                    Console.WriteLine($"[RfbDiag]   窗口 pid={w.Pid} title=[{w.Title}] rect={w.Rect}");
                }
            }

            // 1) 直接命中目标 PID 集合
            var seen = new HashSet<IntPtr>();
            foreach (var w in allTopWindows)
            {
                if (_targetPids.Contains(w.Pid))
                {
                    seen.Add(w.Hwnd);
                    result.Add(new WindowEntry { Handle = w.Hwnd, Title = w.Title, ProcessId = w.Pid, Rect = w.Rect });
                }
            }

            // 2) owned 链属于目标集合（弹窗/模态框）
            foreach (var w in allTopWindows)
            {
                if (seen.Contains(w.Hwnd)) continue;
                if (OwnerChainHitsTarget(w.Hwnd))
                {
                    seen.Add(w.Hwnd);
                    result.Add(new WindowEntry { Handle = w.Hwnd, Title = w.Title, ProcessId = w.Pid, Rect = w.Rect });
                }
            }

            // 3) 跨进程模态链：前台窗口的 owner 链命中目标集合（文件对话框）
            var fg = GetForegroundWindow();
            if (fg != IntPtr.Zero && !seen.Contains(fg))
            {
                var fgEntry = allTopWindows.FirstOrDefault(w => w.Hwnd == fg);
                if (fgEntry.Hwnd != IntPtr.Zero && OwnerChainHitsTarget(fg))
                {
                    seen.Add(fg);
                    result.Add(new WindowEntry { Handle = fg, Title = fgEntry.Title, ProcessId = fgEntry.Pid, Rect = fgEntry.Rect });
                }
            }

            return result;
        }

        /// <summary>窗口的 owner 链（递归 GW_OWNER 到根）是否命中目标 PID 集合。</summary>
        private bool OwnerChainHitsTarget(IntPtr hwnd)
        {
            var owner = hwnd;
            for (int depth = 0; depth < 16 && owner != IntPtr.Zero; depth++)
            {
                GetWindowThreadProcessId(owner, out var pid);
                if (_targetPids.Contains((int)pid))
                    return true;
                owner = GetWindow(owner, GW_OWNER);
            }
            return false;
        }

        private static Rectangle MergeBounds(List<WindowEntry> windows)
        {
            if (windows.Count == 0) return Rectangle.Empty;
            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
            foreach (var w in windows)
            {
                left = Math.Min(left, w.Rect.Left);
                top = Math.Min(top, w.Rect.Top);
                right = Math.Max(right, w.Rect.Right);
                bottom = Math.Max(bottom, w.Rect.Bottom);
            }
            return new Rectangle(left, top, right - left, bottom - top);
        }

        #endregion

        #region 渲染与脏矩形

        /// <summary>逐窗口 PrintWindow 渲染到画布。</summary>
        private void RenderAllWindows(List<WindowEntry> windows, Rectangle bounds, byte[] frame, int stride)
        {
            IntPtr hdcScreen = IntPtr.Zero, hdcMem = IntPtr.Zero, hBitmap = IntPtr.Zero, hOld = IntPtr.Zero;
            try
            {
                hdcScreen = GetDC(IntPtr.Zero);
                hdcMem = CreateCompatibleDC(hdcScreen);

                foreach (var win in windows)
                {
                    try
                    {
                        int winW = win.Rect.Width, winH = win.Rect.Height;
                        if (winW <= 0 || winH <= 0) continue;

                        hBitmap = CreateCompatibleBitmap(hdcScreen, winW, winH);
                        if (hBitmap == IntPtr.Zero) continue;
                        hOld = SelectObject(hdcMem, hBitmap);

                        if (!PrintWindow(win.Handle, hdcMem, PW_RENDERFULLCONTENT))
                        {
                            // 降级 BitBlt
                            BitBlt(hdcMem, 0, 0, winW, winH, hdcScreen, win.Rect.Left, win.Rect.Top, SRCCOPY);
                        }

                        // 将窗口位图贴到画布
                        using (var bmp = System.Drawing.Image.FromHbitmap(hBitmap))
                        {
                            var bmpData = (System.Drawing.Bitmap)bmp;
                            var data = bmpData.LockBits(new System.Drawing.Rectangle(0, 0, winW, winH),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                            try
                            {
                                for (int row = 0; row < winH; row++)
                                {
                                    int dstOff = (win.Rect.Top - bounds.Top + row) * stride + (win.Rect.Left - bounds.Left) * 4;
                                    if (dstOff < 0 || dstOff + winW * 4 > frame.Length) continue;
                                    Marshal.Copy(data.Scan0 + row * data.Stride, frame, dstOff, winW * 4);
                                }
                            }
                            finally
                            {
                                bmpData.UnlockBits(data);
                            }
                        }

                        SelectObject(hdcMem, hOld);
                        DeleteObject(hBitmap);
                        hBitmap = IntPtr.Zero;
                    }
                    catch (Exception ex)
                    {
                        // 单窗口渲染失败跳过（进程可能正在退出/窗口已销毁）
                        Log.Debug(ex, "渲染窗口失败，跳过: {Hwnd}", win.Handle);
                        if (hBitmap != IntPtr.Zero) { DeleteObject(hBitmap); hBitmap = IntPtr.Zero; }
                        SelectObject(hdcMem, hOld);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "渲染合成帧失败");
            }
            finally
            {
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if (hdcMem != IntPtr.Zero) DeleteDC(hdcMem);
                if (hdcScreen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, hdcScreen);
            }
        }

        /// <summary>分块比较新旧帧，生成脏矩形（合并相邻块）。</summary>
        private static void ComputeDirtyRects(byte[] current, byte[] last, int stride, int w, int h, List<Rectangle> dirty)
        {
            const int blockSize = 64;
            int cols = (w + blockSize - 1) / blockSize;
            int rows = (h + blockSize - 1) / blockSize;

            var dirtyBlocks = new List<(int R, int C)>();
            int blockBytes = blockSize * blockSize * 4;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int bx = c * blockSize, by = r * blockSize;
                    int bw = Math.Min(blockSize, w - bx);
                    int bh = Math.Min(blockSize, h - by);
                    if (bw <= 0 || bh <= 0) continue;

                    if (BlockEquals(current, last, stride, bx, by, bw, bh, blockBytes))
                        continue;

                    dirtyBlocks.Add((r, c));
                }
            }

            if (dirtyBlocks.Count == 0) return;

            // 合并为少量矩形：按行合并连续列
            var rects = new List<Rectangle>();
            for (int r = 0; r < rows; r++)
            {
                int start = -1, end = -1;
                foreach (var (br, bc) in dirtyBlocks)
                {
                    if (br != r) continue;
                    if (start == -1) { start = bc; end = bc; }
                    else if (bc == end + 1) end = bc;
                    else
                    {
                        rects.Add(new Rectangle(start * blockSize, r * blockSize,
                            (end - start + 1) * blockSize, blockSize));
                        start = bc; end = bc;
                    }
                }
                if (start != -1)
                    rects.Add(new Rectangle(start * blockSize, r * blockSize,
                        (end - start + 1) * blockSize, blockSize));
            }

            // 合并垂直相邻矩形
            if (rects.Count > 1)
            {
                // 简单合并：若两矩形同行高且相邻列已并；此处做一次"相邻行同列区间"合并
                for (int i = 0; i < rects.Count; i++)
                {
                    for (int j = i + 1; j < rects.Count; j++)
                    {
                        var a = rects[i]; var b = rects[j];
                        if (a.X == b.X && a.Width == b.Width && b.Y == a.Y + a.Height)
                        {
                            rects[i] = new Rectangle(a.X, a.Y, a.Width, a.Height + b.Height);
                            rects.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }

            // 矩形裁剪到画布
            foreach (var r in rects)
            {
                int x = Math.Clamp(r.X, 0, w - 1);
                int y = Math.Clamp(r.Y, 0, h - 1);
                int rw = Math.Clamp(r.Width, 1, w - x);
                int rh = Math.Clamp(r.Height, 1, h - y);
                dirty.Add(new Rectangle(x, y, rw, rh));
            }
        }

        private static bool BlockEquals(byte[] a, byte[] b, int stride, int x, int y, int w, int h, int blockBytes)
        {
            // 快速路径：整块内存比较（仅当块为满宽连续行时）
            bool fullWidth = w * 4 == stride;
            if (fullWidth)
            {
                int off = y * stride + x * 4;
                return MemoryExtensions.SequenceEqual(a.AsSpan(off, w * h * 4), b.AsSpan(off, w * h * 4));
            }

            for (int row = 0; row < h; row++)
            {
                int off = (y + row) * stride + x * 4;
                for (int col = 0; col < w * 4; col++)
                {
                    if (a[off + col] != b[off + col])
                        return false;
                }
            }
            return true;
        }

        #endregion

        public void Dispose()
        {
            lock (_lock)
            {
                _frame = null;
                _lastFrame = null;
                _windows.Clear();
                _targetPids.Clear();
                _dirtyRects.Clear();
            }
        }
    }
}
