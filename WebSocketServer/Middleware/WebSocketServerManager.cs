using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebSocketServer.Middleware;

public class WebSocketServerManager
{
    private ConcurrentDictionary<string, WebSocket> _socket = new ConcurrentDictionary<string, WebSocket>();

    public ConcurrentDictionary<string, WebSocket> GetAllSockets()
    {
        return _socket;
    }

    public string AddSocket(WebSocket socket)
    {
        string ConnID = Guid.NewGuid().ToString();
        _socket.TryAdd(ConnID, socket);
        Console.WriteLine("Connection Added: " + ConnID);

        return ConnID;
    }
}