using System.Net.WebSockets;
using System.Text;
using Serilog;

namespace CJ.Plug.GuacamoleApi.Services
{
    /// <summary>
    /// VNC WebSocket 代理服务
    /// 将浏览器的 WebSocket 连接代理到 VNC 服务器 (TCP 5900)
    /// 用于支持 noVNC 客户端
    /// </summary>
    public class VncWebSocketProxy
    {
        /// <summary>
        /// 处理 WebSocket 连接，代理到 VNC 服务器
        /// </summary>
        public async Task HandleAsync(WebSocket webSocket, string host, int port = 5900)
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            try
            {
                // 连接到 VNC 服务器
                await tcpClient.ConnectAsync(host, port);
                var networkStream = tcpClient.GetStream();

                Log.Information("VNC WebSocket 代理已连接: {Host}:{Port}", host, port);

                // 双向代理
                var wsToTcp = ProxyWebSocketToTcp(webSocket, networkStream);
                var tcpToWs = ProxyTcpToWebSocket(networkStream, webSocket);

                await Task.WhenAny(wsToTcp, tcpToWs);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "VNC WebSocket 代理错误: {Host}:{Port}", host, port);
            }
            finally
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "连接关闭", CancellationToken.None);
                }
                Log.Information("VNC WebSocket 代理已断开: {Host}:{Port}", host, port);
            }
        }

        private async Task ProxyWebSocketToTcp(WebSocket ws, System.Net.Sockets.NetworkStream tcp)
        {
            var buffer = new byte[8192];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    await tcp.WriteAsync(buffer.AsMemory(0, result.Count));
                    await tcp.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "WebSocket -> TCP 代理结束");
            }
        }

        private async Task ProxyTcpToWebSocket(System.Net.Sockets.NetworkStream tcp, WebSocket ws)
        {
            var buffer = new byte[8192];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var bytesRead = await tcp.ReadAsync(buffer);
                    if (bytesRead == 0)
                        break;

                    await ws.SendAsync(
                        new ArraySegment<byte>(buffer, 0, bytesRead),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "TCP -> WebSocket 代理结束");
            }
        }
    }
}
