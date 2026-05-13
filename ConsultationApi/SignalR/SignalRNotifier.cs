using ConsultationApi.Core.DTOs.Notifications;
using ConsultationApi.Core.DTOs.Sessions;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ConsultationApi.SignalR;

public class SignalRNotifier : ISignalRNotifier
{
    private readonly IHubContext<ConsultationHub, IConsultationClient> _hub;

    public SignalRNotifier(IHubContext<ConsultationHub, IConsultationClient> hub)
    {
        _hub = hub;
    }

    public async Task BroadcastChatMessageAsync(Guid sessionId, ChatMessageDto message)
    {
        await _hub.Clients.Group($"session-{sessionId}").ReceiveMessage(message);
    }

    public async Task NotifyUserAsync(Guid userId, NotificationDto notification)
    {
        await _hub.Clients.Group($"user-{userId}").ReceiveNotification(notification);
    }

    public async Task NotifySessionStatusChangedAsync(Guid sessionId, string status)
    {
        await _hub.Clients.Group($"session-{sessionId}").SessionStatusChanged(sessionId, status);
    }
}
