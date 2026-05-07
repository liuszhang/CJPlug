using System.Net.WebSockets;
using System.Text;
using Renci.SshNet;
using Serilog;
using SshConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace CJ.Plug.GuacamoleApi.Services
{
    /// <summary>
    /// SSH WebSocket 代理服务
    /// 将浏览器的 WebSocket 连接代理到 SSH 服务器
    /// 用于支持 xterm.js 终端
    /// </summary>
    public class SshWebSocketProxy
    {
        /// <summary>
        /// 处理 WebSocket 连接，代理到 SSH 服务器
        /// </summary>
        public async Task HandleAsync(WebSocket webSocket, string host, int port, string username, string password)
        {
            SshClient? sshClient = null;
            ShellStream? shellStream = null;

            try
            {
                // 创建 SSH 连接
                var connectionInfo = new SshConnectionInfo(host, port, username,
                    new PasswordAuthenticationMethod(username, password));

                sshClient = new SshClient(connectionInfo);
                sshClient.Connect();

                // 创建交互式 Shell
                shellStream = sshClient.CreateShellStream("xterm-256color", 80, 24, 800, 600, 1024);

                Log.Information("SSH WebSocket 代理已连接: {Host}:{Port} ({User})", host, port, username);

                // 发送初始终端设置
                var initBytes = Encoding.UTF8.GetBytes("\x1b[?25h"); // 显示光标
                await webSocket.SendAsync(
                    new ArraySegment<byte>(initBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                // 双向代理
                var wsToSsh = ProxyWebSocketToSsh(webSocket, shellStream);
                var sshToWs = ProxySshToWebSocket(shellStream, webSocket);

                await Task.WhenAny(wsToSsh, sshToWs);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SSH WebSocket 代理错误: {Host}:{Port}", host, port);

                // 发送错误信息到客户端
                if (webSocket.State == WebSocketState.Open)
                {
                    var errorBytes = Encoding.UTF8.GetBytes($"\r\n\x1b[31m连接失败: {ex.Message}\x1b[0m\r\n");
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(errorBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
            finally
            {
                shellStream?.Dispose();
                sshClient?.Dispose();

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "连接关闭", CancellationToken.None);
                }
                Log.Information("SSH WebSocket 代理已断开: {Host}:{Port}", host, port);
            }
        }

        private async Task ProxyWebSocketToSsh(WebSocket ws, ShellStream ssh)
        {
            var buffer = new byte[4096];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    // 处理特殊字符
                    var input = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // 将输入写入 SSH
                    var bytes = Encoding.UTF8.GetBytes(input);
                    await ssh.WriteAsync(bytes);
                    await ssh.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "WebSocket -> SSH 代理结束");
            }
        }

        private async Task ProxySshToWebSocket(ShellStream ssh, WebSocket ws)
        {
            var buffer = new byte[4096];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var bytesRead = await ssh.ReadAsync(buffer);
                    if (bytesRead == 0)
                        break;

                    await ws.SendAsync(
                        new ArraySegment<byte>(buffer, 0, bytesRead),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "SSH -> WebSocket 代理结束");
            }
        }
    }
}
