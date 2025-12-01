using Microsoft.AspNetCore.SignalR;

namespace TourViet.Hubs;

public class UserHub : Hub
{
    private readonly ILogger<UserHub> _logger;

    public UserHub(ILogger<UserHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to UserHub with connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetHttpContext()?.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from UserHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
