using ConsultationApi.Core.DTOs.Common;
using ConsultationApi.Core.DTOs.Notifications;
using ConsultationApi.Core.Enums;

namespace ConsultationApi.Core.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int page, int pageSize);
    Task<NotificationDto> MarkAsReadAsync(Guid id);
    Task CreateNotificationAsync(Guid userId, NotificationType type, string title, string body);
}
