using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CJ.Plug.Models.LogModels;
using Microsoft.Extensions.Options;
using Serilog;

namespace CJ.Plug.StationApiServer.Services.Rfb
{
    /// <summary>
    /// RFB 单窗口 VNC 服务配置。
    /// </summary>
    public class RfbWindowVncOptions
    {
        public int Port { get; set; } = 5901;
        public bool AutoStart { get; set; } = true;
        /// <summary>帧间隔毫秒（下限 16ms≈60fps 上限）。</summary>
        public int FrameIntervalMs { get; set; } = 66; // ~15fps
        public int JpegQuality { get; set; } = 75;
        /// <summary>绑定后等待目标窗口出现的超时（秒）。</summary>
        public int BindWaitTimeoutSeconds { get; set; } = 60;
    }

    /// <summary>
    /// RFB 3.8 单窗口 VNC 服务端（BackgroundService）。
    /// 帧源：WindowCompositor（L3 多窗口合成 + 脏矩形）；输入：InputInjector。
    /// 支持 Tight(JPEG) 与 Raw 编码、XResize 伪编码（-309）。
    /// </summary>
    public class RfbWindowVncServer : BackgroundService
    {
        private const string PROTOCOL_VERSION = "RFB 003.008\n";
        private const int ENCODING_XRESIZE = -223; // RFB 规范：XResize 伪编码 = 0xFFFFFF21

        private readonly WindowCompositor _compositor;
        private readonly InputInjector _inputInjector;
        private readonly WindowBindRegistry _registry;
        private readonly RfbWindowVncOptions _options;
        private TcpListener? _listener;
        private readonly object _sync = new();
        private volatile bool _frameRequestReceived;
        private DateTime _noWindowSince = DateTime.MinValue;
        private bool _closeNotified;
        private bool _hadFrame;
        private readonly string _stationIp;

        public int Port => _options.Port;

        public RfbWindowVncServer(
            WindowCompositor compositor,
            InputInjector inputInjector,
            WindowBindRegistry registry,
            IOptions<RfbWindowVncOptions> options)
        {
            _compositor = compositor;
            _inputInjector = inputInjector;
            _registry = registry;
            _options = options.Value;
            _stationIp = ResolveLocalIp();
        }

        /// <summary>解析本机 IP（VncWindowClosed 通知用，前端按 IP 匹配才关窗口）。</summary>
        private static string ResolveLocalIp()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                return ip?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.AutoStart)
            {
                Log.Information("RFB 单窗口 VNC 服务未启用 (AutoStart=false)");
                return;
            }

            try
            {
                _listener = new TcpListener(IPAddress.Any, _options.Port);
                _listener.Start();
                Log.Information("RFB 单窗口 VNC 服务已启动: 127.0.0.1:{Port}", _options.Port);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                        _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "接受 RFB 客户端失败");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RFB 单窗口 VNC 服务启动失败（端口 {Port} 可能被占用）", _options.Port);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _listener?.Stop();
            await base.StopAsync(cancellationToken);
        }

        #region 会话

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    Log.Information("RFB 新客户端连接: {Remote}", client.Client.RemoteEndPoint);
                    // 1. 协议版本握手
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(PROTOCOL_VERSION), ct);
                    var clientVersion = new byte[12];
                    await ReadExactlyAsync(stream, clientVersion, ct);
                    if (!Encoding.ASCII.GetString(clientVersion).StartsWith("RFB 003.00", StringComparison.Ordinal))
                    {
                        Log.Warning("不支持的 RFB 协议版本: {Ver}", Encoding.ASCII.GetString(clientVersion));
                        return;
                    }

                    // 2. 安全类型：None (1)
                    await stream.WriteAsync(new byte[] { 1, 1 }, ct);

                    // 3. RFB 3.8 正确时序（noVNC 0.6 逐字节严格校验）：
                    //    3a. 客户端发 1 字节所选安全类型
                    //    3b. 服务端发 4 字节 SecurityResult（=0 成功）——noVNC 无条件读这 4 字节，
                    //        缺失会导致 noVNC 把 ServerInit 前 4 字节当 SecurityResult 读、永久错位超时
                    //    3c. 客户端发 1 字节 ClientInit(shared)
                    var securityType = new byte[1];
                    await ReadExactlyAsync(stream, securityType, ct);
                    if (securityType[0] != 1)
                    {
                        Log.Warning("客户端选择的安全类型不受支持: {Type}", securityType[0]);
                        return;
                    }
                    await stream.WriteAsync(new byte[] { 0, 0, 0, 0 }, ct); // SecurityResult OK

                    var clientInit = new byte[1];
                    await ReadExactlyAsync(stream, clientInit, ct);
                    Log.Information("RFB 客户端初始化: shared={Shared}", clientInit[0]);

                    // 4. ServerInit：尺寸 = 当前合成画布（未绑定时占位 1280x800）
                    int width = 1280, height = 800;
                    var target = _registry.Current;
                    if (target != null)
                    {
                        _compositor.SetTarget(target);
                        // 只算尺寸，不 CaptureFrame——避免消费首帧导致帧循环永无脏矩形
                        _compositor.RefreshSize();
                        if (_compositor.Width > 0 && _compositor.Height > 0)
                        {
                            width = _compositor.Width;
                            height = _compositor.Height;
                        }
                    }
                    await SendServerInitAsync(stream, width, height, ct);

                    Log.Information("RFB 会话建立: {Remote} 画布 {W}x{H}", client.Client.RemoteEndPoint, width, height);

                    // 5. 主循环：读客户端消息（输入/控制）
                    var frameTask = Task.Run(() => SendFramesLoopAsync(stream, ct), ct);
                    await ReadClientMessagesAsync(stream, ct);
                    try { await frameTask; } catch { /* 会话结束 */ }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Debug(ex, "RFB 会话结束");
                }
                finally
                {
                    Log.Information("RFB 会话关闭");
                }
            }
        }

        private async Task SendServerInitAsync(NetworkStream stream, int width, int height, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            // Framebuffer 尺寸
            ms.Write(BigEndian16(width));
            ms.Write(BigEndian16(height));
            // PixelFormat：32bpp true color, little-endian（BGRX）
            ms.WriteByte(32);            // bits-per-pixel
            ms.WriteByte(24);            // depth
            ms.WriteByte(0);             // big-endian-flag
            ms.WriteByte(1);             // true-colour-flag
            ms.Write(BigEndian16(255));  // red-max
            ms.Write(BigEndian16(255));  // green-max
            ms.Write(BigEndian16(255));  // blue-max
            ms.WriteByte(16);            // red-shift
            ms.WriteByte(8);             // green-shift
            ms.WriteByte(0);             // blue-shift
            ms.WriteByte(0);             // padding (3 字节)
            ms.WriteByte(0);
            ms.WriteByte(0);
            // 桌面名
            var name = Encoding.UTF8.GetBytes("CJ.Plug Window VNC");
            ms.Write(BigEndian32(name.Length));
            ms.Write(name);
            await stream.WriteAsync(ms.ToArray(), ct);
        }

        /// <summary>
        /// 帧推送循环：绑定目标 + 脏矩形 → FramebufferUpdate（Tight JPEG / Raw / XResize）。
        /// </summary>
        private async Task SendFramesLoopAsync(NetworkStream stream, CancellationToken ct)
        {
            var interval = Math.Max(16, _options.FrameIntervalMs);
            var lastFrameTime = DateTime.MinValue;
            int fbWidth = 1280, fbHeight = 800;
            WindowTarget? lastTarget = null;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // RFB 规范：服务端须在收到 FramebufferUpdateRequest 后才能推送帧
                    if (!_frameRequestReceived)
                    {
                        await Task.Delay(interval, ct);
                        continue;
                    }

                    var target = _registry.Current;
                    if (target == null)
                    {
                        // 未绑定：等待（前端显示等待态由 noVNC 黑屏承载）
                        lastTarget = null;
                        await Task.Delay(interval, ct);
                        continue;
                    }

                    // 目标变化才重新绑定（避免每帧全量重绘）
                    if (target != lastTarget)
                    {
                        _compositor.SetTarget(target);
                        lastTarget = target;
                        // 新目标：重置画面历史与关闭通知状态
                        _hadFrame = false;
                        _noWindowSince = DateTime.MinValue;
                        _closeNotified = false;
                    }

                    bool hasFrame = _compositor.CaptureFrame();
                    if (hasFrame)
                    {
                        _hadFrame = true; // 出现过画面（目标窗口真实存在过）
                    }
                    if (!hasFrame)
                    {
                        // 无画面：目标可能尚未出窗。绑定等待超时后解绑。
                        if (_registry.IsExpired(TimeSpan.FromSeconds(_options.BindWaitTimeoutSeconds)))
                        {
                            Log.Information("等待目标窗口超时，清除绑定");
                            _registry.Clear();
                        }
                        // 仅当"曾经出现过画面"后窗口再消失（≥3 秒）才判定进程退出：
                        // notepad 启动等待期（从未出画面）不触发，避免可视化窗口刚打开就被关
                        else if (_hadFrame && _noWindowSince == DateTime.MinValue)
                        {
                            _noWindowSince = DateTime.Now;
                        }
                        // 目标窗口消失 ≥ 3 秒（进程已退出）→ 通知前端自动关闭可视化窗口
                        else if (_hadFrame && !_closeNotified
                                 && _noWindowSince != DateTime.MinValue
                                 && DateTime.Now - _noWindowSince > TimeSpan.FromSeconds(3))
                        {
                            _closeNotified = true;
                            try
                            {
                                var payload = System.Text.Json.JsonSerializer.Serialize(new
                                {
                                    StationIp = _stationIp,
                                    SessionKey = _registry.SessionKey
                                });
                                CLog.Information(payload, null, null, null, null, LogTypeEnum.VncWindowClosed);
                                Log.Information("目标窗口消失超过 3 秒，已通知前端关闭可视化窗口");
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "发送 VncWindowClosed 通知失败");
                            }
                        }
                        await Task.Delay(interval, ct);
                        continue;
                    }
                    // 有画面：重置无窗口计时
                    _noWindowSince = DateTime.MinValue;
                    _closeNotified = false;

                    // 画布尺寸变化 → XResize 伪编码
                    int newW = _compositor.Width, newH = _compositor.Height;
                    if (newW != fbWidth || newH != fbHeight)
                    {
                        await SendResizeAsync(stream, newW, newH, ct);
                        fbWidth = newW;
                        fbHeight = newH;
                    }

                    var frame = _compositor.Frame;
                    var dirtyRects = _compositor.DirtyRects;
                    if (frame == null || dirtyRects.Count == 0)
                    {
                        await Task.Delay(interval, ct);
                        continue;
                    }

                    // 限帧
                    var now = DateTime.Now;
                    if ((now - lastFrameTime).TotalMilliseconds < interval)
                    {
                        await Task.Delay(interval - (int)(now - lastFrameTime).TotalMilliseconds, ct);
                    }
                    lastFrameTime = DateTime.Now;

                    await SendFramebufferUpdateAsync(stream, frame, _compositor.Stride, dirtyRects, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    // 客户端已断开（写流失败）→ 退出本连接帧循环
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 单次帧推送失败不终止循环（避免 Task.Run 静默吞异常导致永不再推帧）
                    Console.WriteLine($"[RfbDiag] 帧循环异常: {ex.Message}");
                    Log.Warning(ex, "帧推送循环单次异常，继续");
                    await Task.Delay(interval, ct);
                }
            }
        }

        private async Task SendResizeAsync(NetworkStream stream, int w, int h, CancellationToken ct)
        {
            // RFB XResize/DesktopSize 伪编码（noVNC 0.6 源码铁证）：
            //   rect 头的 width/height 字段 = 新尺寸（U16），无额外 payload！
            //   noVNC: case pseudoEncodingDesktopSize: this._resize(this._FBU.width, this._FBU.height)
            // 旧实现发 w=0,h=0 + 8 字节 payload → noVNC resize 到 0x0 → 黑屏
            using var ms = new MemoryStream();
            ms.WriteByte(0);   // type FramebufferUpdate
            ms.WriteByte(0);   // padding
            ms.Write(BigEndian16(1)); // 1 rect
            ms.Write(BigEndian16(0)); // x
            ms.Write(BigEndian16(0)); // y
            ms.Write(BigEndian16(w)); // width = 新宽
            ms.Write(BigEndian16(h)); // height = 新高
            ms.Write(BigEndian32(ENCODING_XRESIZE));
            await stream.WriteAsync(ms.ToArray(), ct);
        }

        private async Task SendFramebufferUpdateAsync(NetworkStream stream, byte[] frame, int stride,
            IReadOnlyList<Rectangle> dirtyRects, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            ms.WriteByte(0); // type
            ms.WriteByte(0); // padding
            ms.Write(BigEndian16((ushort)Math.Min(dirtyRects.Count, ushort.MaxValue)));

            int sent = 0;
            foreach (var rect in dirtyRects)
            {
                if (sent >= ushort.MaxValue) break;
                int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;

                ms.Write(BigEndian16(x));
                ms.Write(BigEndian16(y));
                ms.Write(BigEndian16(w));
                ms.Write(BigEndian16(h));
                ms.Write(BigEndian32(TightEncoder.EncodingTight));

                // Tight JPEG（小矩形退化 Raw 更省 CPU 的情况：宽高 < 32 用 Raw 避免 JPEG 开销）
                byte[]? payload = (w * h < 32 * 32)
                    ? TightEncoder.EncodeRaw(frame, stride, x, y, w, h)
                    : TightEncoder.EncodeTightJpeg(frame, stride, x, y, w, h, _options.JpegQuality);
                if (payload == null)
                {
                    // 编码失败 → Raw 兜底
                    payload = TightEncoder.EncodeRaw(frame, stride, x, y, w, h);
                }
                ms.Write(payload);
                sent++;
            }

            await stream.WriteAsync(ms.ToArray(), ct);
        }

        private async Task ReadClientMessagesAsync(NetworkStream stream, CancellationToken ct)
        {
            var header = new byte[1];
            while (!ct.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(header, ct);
                if (read == 0) break;

                //Log.Information("RFB 收到客户端消息类型: {Type}", header[0]);

                switch (header[0])
                {
                    case 0: // SetPixelFormat
                    {
                        var buf = new byte[19];
                        if (await ReadExactlyOrBreakAsync(stream, buf, ct) == 0) return;
                        Log.Information("RFB SetPixelFormat: bpp={Bpp} depth={Depth}", buf[0], buf[1]);
                        // 仅支持 32bpp；其余格式断开（noVNC 默认 32bpp 不会发）
                        break;
                    }
                    case 2: // SetEncodings
                    {
                        // body = padding(1) + count U16(2) + encodings(count*4)
                        var buf = new byte[3];
                        if (await ReadExactlyOrBreakAsync(stream, buf, ct) == 0) return;
                        int count = (buf[1] << 8) | buf[2];
                        var encBuf = new byte[count * 4];
                        if (await ReadExactlyOrBreakAsync(stream, encBuf, ct) == 0) return;
                        var encs = new List<int>();
                        for (int i = 0; i < count; i++)
                        {
                            int e = (encBuf[i * 4] << 24) | (encBuf[i * 4 + 1] << 16) | (encBuf[i * 4 + 2] << 8) | encBuf[i * 4 + 3];
                            encs.Add(e);
                        }
                        Log.Information("RFB SetEncodings: {Encodings}", string.Join(",", encs));
                        break;
                    }
                    case 3: // FramebufferUpdateRequest
                    {
                        // 消息总长 10 字节：type(1,已读) + incremental(1) + x(2) + y(2) + w(2) + h(2) = body 9 字节
                        var buf = new byte[9];
                        if (await ReadExactlyOrBreakAsync(stream, buf, ct) == 0) return;
                        // incremental 标志：buf[0]；请求区域忽略（我们按脏矩形全推）
                        _frameRequestReceived = true;
                        Log.Information("RFB FramebufferUpdateRequest: incremental={Inc} x={X} y={Y} w={W} h={H}",
                            buf[0], (buf[1] << 8) | buf[2], (buf[3] << 8) | buf[4],
                            (buf[5] << 8) | buf[6], (buf[7] << 8) | buf[8]);
                        break;
                    }
                    case 4: // KeyEvent
                    {
                        var buf = new byte[7];
                        if (await ReadExactlyOrBreakAsync(stream, buf, ct) == 0) return;
                        bool down = buf[0] != 0;
                        uint keysym = (uint)((buf[3] << 24) | (buf[4] << 16) | (buf[5] << 8) | buf[6]);
                        Log.Information("RFB KeyEvent: down={Down} keysym=0x{Keysym:X}", down, keysym);
                        _inputInjector.InjectKey(keysym, down);
                        break;
                    }
                    case 5: // PointerEvent
                    {
                        var buf = new byte[5];
                        if (await ReadExactlyOrBreakAsync(stream, buf, ct) == 0) return;
                        byte mask = buf[0];
                        int x = (buf[1] << 8) | buf[2];
                        int y = (buf[3] << 8) | buf[4];
                        //Log.Information("RFB PointerEvent: mask={Mask} x={X} y={Y}", mask, x, y);
                        _inputInjector.InjectPointer(mask, x, y);
                        break;
                    }
                    case 6: // ClientCutText（忽略）
                    {
                        // body = padding(3) + length(4) + text
                        var buf = new byte[7];
                        if (await ReadExactlyOrBreakAsync(stream, buf, ct) == 0) return;
                        int len = (buf[3] << 24) | (buf[4] << 16) | (buf[5] << 8) | buf[6];
                        if (len > 0)
                        {
                            var text = new byte[len];
                            if (await ReadExactlyOrBreakAsync(stream, text, ct) == 0) return;
                        }
                        Log.Information("RFB ClientCutText: len={Len}", len);
                        break;
                    }
                    default:
                        Log.Warning("未知 RFB 消息类型: {Type}", header[0]);
                        return;
                }
            }
        }

        #endregion

        #region Helpers

        private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
                if (read == 0) throw new EndOfStreamException();
                offset += read;
            }
        }

        private static async Task<int> ReadExactlyOrBreakAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
                if (read == 0) return 0;
                offset += read;
            }
            return buffer.Length;
        }

        private static byte[] BigEndian16(int value) =>
            [(byte)((value >> 8) & 0xFF), (byte)(value & 0xFF)];

        private static byte[] BigEndian32(int value) =>
            [(byte)((value >> 24) & 0xFF), (byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF)];

        #endregion
    }
}
