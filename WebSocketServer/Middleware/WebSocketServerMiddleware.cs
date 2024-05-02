using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using WebSocketServer.Model;

namespace WebSocketServer.Middleware;

public class WebSocketServerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebSocketServerManager _manager;

    public WebSocketServerMiddleware(RequestDelegate next, WebSocketServerManager manager)
    {
        _next = next;
        _manager = manager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using WebSocket webSocket= await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("WebSocket Connected");

            string ConnID = _manager.AddSocket(webSocket);
            await SendConnIDAsync(webSocket, ConnID);

            await ReceiveMessage(webSocket, async (result, buffer) => {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    Console.WriteLine("Message Received");
                    Console.WriteLine($"Message: {Encoding.UTF8.GetString(buffer, 0, buffer.Length)}");
                    await RouteJSONMessageAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    return;   
                }else if(result.MessageType == WebSocketMessageType.Close)
                {
                    string id = _manager.GetAllSockets().FirstOrDefault(_ => _.Value == webSocket).Key;
                    Console.WriteLine("Received Close message");
                    _manager.GetAllSockets().TryRemove(id, out WebSocket sock);
                    await sock.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    return;
                }
            });
        }else
        {
            Console.WriteLine("Hello from the 2rd request delegate.");
            await _next(context);
        }
    }


    public static void WriteRequestParam(HttpContext context){
        Console.WriteLine("Request Method: "+ context.Request.Method);
        Console.WriteLine("Request Protocol: "+ context.Request.Protocol);

        if (context.Request.Headers != null)
        {
            foreach (var item in context.Request.Headers)
            {
                Console.WriteLine($"---> {item.Key}:{item.Value}");
            }
        }
    }

    private static async Task SendConnIDAsync(WebSocket socket, string connID)
    {
        var buffer = Encoding.UTF8.GetBytes("ConnID: " + connID);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
    {
        var buffer = new byte[1024 * 4];
        // while (socket.State != WebSocketState.Open)
        // {
        //     var result = await socket.ReceiveAsync(
        //         buffer: new ArraySegment<byte>(buffer), 
        //         cancellationToken: CancellationToken.None
        //     );

        //     handleMessage(result, buffer);
        // }

        var receiveResult = await socket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            // await webSocket.SendAsync(
            //     new ArraySegment<byte>(buffer, 0, receiveResult.Count),
            //     receiveResult.MessageType,
            //     receiveResult.EndOfMessage,
            //     CancellationToken.None);

            receiveResult = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            handleMessage(receiveResult, buffer);
        }

        await socket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }

    private async Task RouteJSONMessageAsync(string message)
    {
        var routeObj = JsonSerializer.Deserialize<MessageSender>(message);

        if (routeObj.To != null && Guid.TryParse(routeObj.To.ToString(), out Guid guidOutput))
        {
            Console.WriteLine("Targeted");
            var sock = _manager.GetAllSockets().FirstOrDefault(item => item.Key == routeObj.To.ToString());

            if (sock.Value != null)
            {
                if (sock.Value.State == WebSocketState.Open)
                {
                    await sock.Value.SendAsync(Encoding.UTF8.GetBytes(
                        routeObj.Message.ToString()),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );

                }
            }else
            {
                Console.WriteLine($"Invalid recipient");
            }

        }else
        {
            Console.WriteLine("Broadcast");
            foreach (var item in _manager.GetAllSockets())
            {
                if(item.Value.State == WebSocketState.Open)
                {
                    await item.Value.SendAsync(
                        Encoding.UTF8.GetBytes(routeObj.Message.ToString()),
                        WebSocketMessageType.Text, true, CancellationToken.None
                    );
                }
            }
        }
    }
}