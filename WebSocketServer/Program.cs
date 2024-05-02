using System.Net.WebSockets;
using WebSocketServer.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebSocketManager();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseWebSocketServer();

app.Run();
