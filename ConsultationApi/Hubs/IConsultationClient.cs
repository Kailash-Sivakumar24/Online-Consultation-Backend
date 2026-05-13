using ConsultationApi.Core.DTOs.Notifications;
using ConsultationApi.Core.DTOs.Sessions;

namespace ConsultationApi.Hubs;

public interface IConsultationClient
{
    Task ReceiveMessage(ChatMessageDto message);
    Task ReceiveNotification(NotificationDto notification);
    Task SessionStatusChanged(Guid sessionId, string status);
}
