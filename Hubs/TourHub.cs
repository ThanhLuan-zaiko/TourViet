using Microsoft.AspNetCore.SignalR;
using TourViet.DTOs;

namespace TourViet.Hubs;

/// <summary>
/// SignalR Hub for real-time tour updates.
/// Broadcasts tour creation, updates, and deletion events to connected clients.
/// </summary>
public class TourHub : Hub
{
    private readonly ILogger<TourHub> _logger;

    public TourHub(ILogger<TourHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a page-specific group to receive relevant tour updates.
    /// </summary>
    /// <param name="pageType">
    /// Type of page: "all", "trending", "domestic", or "international"
    /// </param>
    public async Task JoinPageGroup(string pageType)
    {
        var groupName = $"tours_{pageType.ToLower()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined group: {GroupName}", 
            Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a page-specific group when navigating away.
    /// </summary>
    /// <param name="pageType">
    /// Type of page: "all", "trending", "domestic", or "international"
    /// </param>
    public async Task LeavePageGroup(string pageType)
    {
        var groupName = $"tours_{pageType.ToLower()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left group: {GroupName}", 
            Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to TourHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", 
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
