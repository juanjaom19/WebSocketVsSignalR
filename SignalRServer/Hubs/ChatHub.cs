using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SignalRServer.Model;

namespace SignalRServer.Hubs;

public class ChatHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine("--> Connection Stablished "+ Context.ConnectionId);
        Clients.Client(Context.ConnectionId).SendAsync("ReceiveConnID", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task SendMessageAsync(string message)
    {
        var routeObj = JsonSerializer.Deserialize<MessageSender>(message);
        string toClient = routeObj.To;
        Console.WriteLine("Message Recieved on: " + Context.ConnectionId);

        if (string.IsNullOrEmpty(toClient))
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }else
        {
            await Clients.Client(toClient).SendAsync("ReceiveMessage", message);
        }
    }
}