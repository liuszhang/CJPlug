using System.Net.WebSockets;
using System.Text;
using Serilog;

namespace CJ.Plug.GuacamoleApi.Services
{
    /// <summary>
    /// Capture WebSocket 代理服务
    /// 将浏览器的 WebSocket 连接代理到 StationApiServer 的 Capture WebSocket (WS-to-WS)
    /// </summary>
    public class CaptureWebSocketProxy
    {
        /// <summary>
        /// 处理 WebSocket 连接，代理到 Station 的 Capture 端点
        /// </summary>
        public async Task HandleAsync(WebSocket clientWs, string stationHost, string processName, int fps, int stationPort = 7660)
        {
            using var stationWs = new ClientWebSocket();
            try
            {
                // 连接到 Station 的 Capture WebSocket 端点
                var uri = new Uri($"ws://{stationHost}:{stationPort}/api/station/remote/capture?processName={Uri.EscapeDataString(processName)}&fps={fps}");
                await stationWs.ConnectAsync(uri, CancellationToken.None);

                Log.Information("Capture WebSocket 代理已连接: {StationHost}, process={ProcessName}, fps={Fps}", stationHost, processName, fps);

                // 双向代理
                var clientToStation = ProxyWebSocket(clientWs, stationWs, "Client->Station");
                var stationToClient = ProxyWebSocket(stationWs, clientWs, "Station->Client");

                await Task.WhenAny(clientToStation, stationToClient);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Capture WebSocket 代理错误: {StationHost}, process={ProcessName}", stationHost, processName);
            }
            finally
            {
                if (clientWs.State == WebSocketState.Open)
                {
                    await clientWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "连接关闭", CancellationToken.None);
                }
                if (stationWs.State == WebSocketState.Open)
                {
                    await stationWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "连接关闭", CancellationToken.None);
                }
                Log.Information("Capture WebSocket 代理已断开: {StationHost}, process={ProcessName}", stationHost, processName);
            }
        }

        private async Task ProxyWebSocket(WebSocket source, WebSocket destination, string direction)
        {
            var buffer = new byte[65536]; // 64KB buffer for image data
            try
            {
                while (source.State == WebSocketState.Open && destination.State == WebSocketState.Open)
                {
                    var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    await destination.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        result.MessageType,
                        result.EndOfMessage,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Capture 代理 {Direction} 结束", direction);
            }
        }
    }
}
