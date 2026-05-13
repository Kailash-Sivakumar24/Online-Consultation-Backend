using ConsultationApi.Core.DTOs.Sessions;
using ConsultationApi.Core.DTOs.Notifications;

namespace ConsultationApi.Core.Interfaces;

public interface ISignalRNotifier
{
    Task BroadcastChatMessageAsync(Guid sessionId, ChatMessageDto message);
    Task NotifyUserAsync(Guid userId, NotificationDto notification);
    Task NotifySessionStatusChangedAsync(Guid sessionId, string status);
}
