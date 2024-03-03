using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;

Console.WriteLine("ConsoleServer...");

var concurrentDictionary = new ConcurrentDictionary<int, WebSocket>();

ThreadPool.QueueUserWorkItem(async (obj) =>
{
    var listener = new HttpListener();
    listener.Prefixes.Add("http://localhost:8080/");
    listener.Start();
    while (listener.IsListening)
    {
        var context = await listener.GetContextAsync();
        ThreadPool.QueueUserWorkItem(async (obj) =>
        {
            if (context!.Request.IsWebSocketRequest)
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                concurrentDictionary.TryAdd(webSocket.GetHashCode(), webSocket);
                try
                {
                    var buffer = new byte[1024 * 4];
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            Console.WriteLine($"Received: {receivedMessage}");
                            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, CancellationToken.None);
                        }
                        //await Task.Delay(1000);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("WebSocket Exception {0}", e.Message);
                    await webSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, webSocket.CloseStatusDescription, CancellationToken.None);
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }, context);
    }
});

Console.ReadLine();

await Parallel.ForEachAsync(concurrentDictionary, async (dictionary, cancellationToken) =>
{
    try
    {
        var webSocket = dictionary.Value;
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatusDescription, cancellationToken);
        webSocket.Dispose();
    }
    catch (Exception e)
    {
        Console.Error.WriteLine("Close Exception {0}", e.Message);
    }
});

concurrentDictionary.Clear();

Console.WriteLine("...ConsoleServer");

Console.WriteLine("Press Any Key To Exit.");
Console.ReadLine();
Environment.Exit(0);