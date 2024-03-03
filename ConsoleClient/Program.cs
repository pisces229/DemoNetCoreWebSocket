using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

Console.WriteLine("ConsoleClient...");

await Task.Delay(1000);

var concurrentDictionary = new ConcurrentDictionary<int, ClientWebSocket>();

await Parallel.ForEachAsync(Enumerable.Range(0, 3), async (i, cancellationToken) =>
{
    Console.WriteLine("Connection {0}", i);
    var clientWebSocket = new ClientWebSocket();
    await clientWebSocket.ConnectAsync(new Uri("ws://localhost:8080/"), cancellationToken);
    // Receive
    ThreadPool.QueueUserWorkItem(async (obj) =>
    {
        try
        {
            while (clientWebSocket.State == WebSocketState.Open)
            {
                var receiveBuffer = new byte[1024 * 4];
                var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                    Console.WriteLine($"Received from server: {receivedMessage}");
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Connection {0} Close", i);
                }
                await Task.Delay(1000);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Connection {0} Exception {1}", i, e.Message);
            //await clientWebSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, clientWebSocket.CloseStatusDescription, CancellationToken.None);
        }
    }, clientWebSocket);
    // Send
    ThreadPool.QueueUserWorkItem(async (obj) =>
    {
        try
        {
            while (clientWebSocket.State == WebSocketState.Open)
            {
                //var message = Console.ReadLine()!;
                //var messageBytes = Encoding.UTF8.GetBytes(message);
                var messageBytes = Encoding.UTF8.GetBytes($"Connection {i}");
                await clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
                await Task.Delay(1000);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Connection {0} Exception {1}", i, e.Message);
            //await clientWebSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, clientWebSocket.CloseStatusDescription, CancellationToken.None);
        }
    }, clientWebSocket);
    concurrentDictionary.TryAdd(clientWebSocket.GetHashCode(), clientWebSocket);
});

Console.ReadLine();

await Parallel.ForEachAsync(concurrentDictionary, async (dictionary, cancellationToken) =>
{
    try
    {
        var clientWebSocket = dictionary.Value;
        await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, clientWebSocket.CloseStatusDescription, cancellationToken);
        clientWebSocket.Dispose();
    }
    catch (Exception e)
    {
        Console.Error.WriteLine("Close Exception {0}", e.Message);
    }
});

concurrentDictionary.Clear();

Console.WriteLine("...ConsoleClient");

Console.WriteLine("Press Any Key To Exit.");
Console.ReadLine();
Environment.Exit(0);