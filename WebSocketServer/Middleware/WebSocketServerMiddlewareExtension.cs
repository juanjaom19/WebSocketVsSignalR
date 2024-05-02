
namespace WebSocketServer.Middleware;

public static class WebSocketServerMiddlewareExtension
{
    public static IApplicationBuilder UseWebSocketServer(this IApplicationBuilder app)
    {
        return app.UseMiddleware<WebSocketServerMiddleware>();
    } 

    public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketServerManager>();
        return services;
    }
}