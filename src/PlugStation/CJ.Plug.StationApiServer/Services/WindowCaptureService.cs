using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Serilog;

namespace CJ.Plug.StationApiServer.Services
{
    /// <summary>
    /// 窗口捕获服务
    /// 使用 Win32 PrintWindow P/Invoke + System.Drawing 捕获窗口画面并以 JPEG 帧通过 WebSocket 推送
    /// </summary>
    public class WindowCaptureService
    {
        #region Win32 P/Invoke

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ho);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        private const uint PW_RENDERFULLCONTENT = 0x00000002;
        private const uint SRCCOPY = 0x00CC0020;

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        #endregion

        // 按连接 ID 跟踪捕获状态
        private readonly ConcurrentDictionary<string, CaptureSession> _sessions = new();

        /// <summary>
        /// 捕获会话信息
        /// </summary>
        private class CaptureSession
        {
            public IntPtr WindowHandle { get; set; }
            public string WindowTitle { get; set; } = "";
            public int Fps { get; set; } = 15;
            public bool Paused { get; set; }
            public CancellationTokenSource Cts { get; set; } = new();
        }

        /// <summary>
        /// 可捕获的窗口信息
        /// </summary>
        public record WindowInfo(IntPtr Handle, string Title, string ProcessName, int ProcessId);

        /// <summary>
        /// 列出所有可见的、有标题的窗口
        /// </summary>
        public List<WindowInfo> GetCapturableWindows()
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd))
                    return true;

                int length = GetWindowTextLength(hwnd);
                if (length == 0)
                    return true;

                var sb = new StringBuilder(length + 1);
                GetWindowText(hwnd, sb, sb.Capacity);
                var title = sb.ToString();

                if (string.IsNullOrWhiteSpace(title))
                    return true;

                try
                {
                    uint processId;
                    GetWindowThreadProcessId(hwnd, out processId);
                    var proc = Process.GetProcessById((int)processId);
                    windows.Add(new WindowInfo(hwnd, title, proc.ProcessName, (int)processId));
                }
                catch
                {
                    // 进程可能已退出
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// 根据进程名查找窗口句柄
        /// </summary>
        public IntPtr FindWindowByProcessName(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var proc in processes)
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                    return proc.MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 捕获单帧为 JPEG 字节数组
        /// </summary>
        public byte[]? CaptureFrame(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;

            GetWindowRect(hwnd, out var rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                return null;

            IntPtr hdcScreen = IntPtr.Zero;
            IntPtr hdcMem = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hOld = IntPtr.Zero;

            try
            {
                hdcScreen = GetDC(IntPtr.Zero);
                hdcMem = CreateCompatibleDC(hdcScreen);
                hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
                hOld = SelectObject(hdcMem, hBitmap);

                // 使用 PrintWindow 捕获（包括被遮挡的窗口）
                if (!PrintWindow(hwnd, hdcMem, PW_RENDERFULLCONTENT))
                {
                    // 降级到 BitBlt
                    BitBlt(hdcMem, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY);
                }

                SelectObject(hdcMem, hOld);

                using var bmp = Image.FromHbitmap(hBitmap);
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "捕获窗口帧失败");
                return null;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if (hdcMem != IntPtr.Zero) DeleteDC(hdcMem);
                if (hdcScreen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, hdcScreen);
            }
        }

        /// <summary>
        /// 处理窗口捕获 WebSocket 连接
        /// </summary>
        public async Task HandleWebSocketAsync(WebSocket ws, string processName, int fps, string connectionId)
        {
            fps = Math.Clamp(fps, 1, 60);

            var hwnd = FindWindowByProcessName(processName);
            if (hwnd == IntPtr.Zero)
            {
                // 尝试直接用进程名列表搜索
                var windows = GetCapturableWindows();
                var match = windows.FirstOrDefault(w =>
                    w.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
                if (match.Handle != IntPtr.Zero)
                    hwnd = match.Handle;
            }

            if (hwnd == IntPtr.Zero)
            {
                var errBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type = "error", message = $"未找到进程 '{processName}' 的窗口" }));
                await ws.SendAsync(errBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "窗口未找到", CancellationToken.None);
                return;
            }

            // 获取窗口尺寸
            GetWindowRect(hwnd, out var rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            var session = new CaptureSession
            {
                WindowHandle = hwnd,
                Fps = fps,
                Paused = false
            };
            _sessions[connectionId] = session;

            try
            {
                // 发送 info 消息
                var infoJson = JsonSerializer.Serialize(new
                {
                    type = "info",
                    window = processName,
                    fps,
                    width,
                    height
                });
                await ws.SendAsync(Encoding.UTF8.GetBytes(infoJson), WebSocketMessageType.Text, true, CancellationToken.None);

                // 启动捕获循环
                var captureTask = RunCaptureLoop(ws, session);

                // 读取客户端消息（控制指令）
                var receiveTask = ReceiveClientMessages(ws, session);

                await Task.WhenAny(captureTask, receiveTask);
            }
            finally
            {
                session.Cts.Cancel();
                _sessions.TryRemove(connectionId, out _);
            }
        }

        private async Task RunCaptureLoop(WebSocket ws, CaptureSession session)
        {
            var ct = session.Cts.Token;

            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                if (!session.Paused)
                {
                    var frame = CaptureFrame(session.WindowHandle);
                    if (frame != null)
                    {
                        try
                        {
                            await ws.SendAsync(frame, WebSocketMessageType.Binary, true, ct);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }

                await Task.Delay(1000 / session.Fps, ct);
            }
        }

        private async Task ReceiveClientMessages(WebSocket ws, CaptureSession session)
        {
            var buffer = new byte[1024];
            var ct = session.Cts.Token;

            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        session.Cts.Cancel();
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        HandleClientMessage(text, session);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private static void HandleClientMessage(string text, CaptureSession session)
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeProp))
                {
                    var type = typeProp.GetString();
                    switch (type)
                    {
                        case "pause":
                            session.Paused = true;
                            break;
                        case "resume":
                            session.Paused = false;
                            break;
                        case "fps":
                            if (root.TryGetProperty("value", out var fpsProp) && fpsProp.TryGetInt32(out var fpsVal))
                            {
                                session.Fps = Math.Clamp(fpsVal, 1, 60);
                            }
                            break;
                    }
                }
            }
            catch
            {
                // 忽略无效 JSON
            }
        }

        /// <summary>
        /// 移除捕获会话（连接断开时调用）
        /// </summary>
        public void RemoveSession(string connectionId)
        {
            if (_sessions.TryRemove(connectionId, out var session))
            {
                session.Cts.Cancel();
            }
        }
    }
}
