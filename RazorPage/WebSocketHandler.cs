using Microsoft.AspNetCore.Http.Connections;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace RazorPage
{
    public class WebSocketHandler(ILogger<WebSocketHandler> logger)
    {
        private readonly ConcurrentDictionary<int, WebSocket> _webSockets = new();
        public async Task ProcessWebSocket(WebSocket webSocket)
        {
            _webSockets.TryAdd(webSocket.GetHashCode(), webSocket);
            var buffer = new byte[1024 * 4];
            var webSocketReceiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var userName = "anonymous";
            while (!webSocketReceiveResult.CloseStatus.HasValue)
            {
                var cmd = Encoding.UTF8.GetString(buffer, 0, webSocketReceiveResult.Count);
                if (!string.IsNullOrEmpty(cmd))
                {
                    //logger.LogInformation(cmd);
                    if (cmd.StartsWith("/USER "))
                    {
                        userName = cmd[6..];
                        Broadcast(webSocket.GetHashCode(), $"{userName} join the room.");
                    }
                    else
                    {
                        Broadcast(webSocket.GetHashCode(), $"{userName}: {cmd}");
                    }
                }
                webSocketReceiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(
                webSocketReceiveResult.CloseStatus.Value, 
                webSocketReceiveResult.CloseStatusDescription, 
                CancellationToken.None);
            _webSockets.TryRemove(webSocket.GetHashCode(), out _);
            Broadcast(webSocket.GetHashCode(), $"{userName} left the room.");
        }
        private void Broadcast(int hashcode, string message)
        {
            logger.LogInformation("{hashcode} Broadcast: '{message}'", hashcode.ToString().PadLeft(8, '0'), message);
            var buff = Encoding.UTF8.GetBytes($"{DateTime.Now:HH:mm:ss} {message}");
            var data = new ArraySegment<byte>(buff, 0, buff.Length);
            Parallel.ForEach(_webSockets.Values, async (webSocket) =>
            {
                logger.LogInformation("{HashCode}: {State}", webSocket.GetHashCode().ToString().PadLeft(8, '0'), webSocket.State.ToString());
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            });
        }
    }
}
