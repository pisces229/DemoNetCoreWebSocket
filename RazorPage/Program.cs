using RazorPage;
using System.Net;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var wsHandler = context.RequestServices.GetRequiredService<WebSocketHandler>();
            await wsHandler.ProcessWebSocket(webSocket);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
    else
    {
        await next();
    }
});

app.MapRazorPages();

app.Run();
