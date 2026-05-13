using ConsultationApi.Core.DTOs.Common;
using ConsultationApi.Core.DTOs.Notifications;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;

namespace ConsultationApi.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ISignalRNotifier _notifier;

    public NotificationService(IUnitOfWork uow, ICurrentUserService currentUser, ISignalRNotifier notifier)
    {
        _uow = uow;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int page, int pageSize)
    {
        var all = await _uow.Notifications.FindAsync(n => n.UserId == _currentUser.UserId);
        var ordered = all.OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAt).ToList();

        var total = ordered.Count;
        var data = ordered.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(MapDto).ToList();

        return new PagedResult<NotificationDto>
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<NotificationDto> MarkAsReadAsync(Guid id)
    {
        var notification = await _uow.Notifications.GetByIdAsync(id)
            ?? throw new NotFoundException("Notification", id);

        if (notification.UserId != _currentUser.UserId)
            throw new ForbiddenException("Access denied.");

        notification.IsRead = true;
        await _uow.Notifications.UpdateAsync(notification);
        await _uow.CommitAsync();

        return MapDto(notification);
    }

    public async Task CreateNotificationAsync(Guid userId, NotificationType type, string title, string body)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            IsRead = false
        };
        await _uow.Notifications.AddAsync(notification);
        await _uow.CommitAsync();

        await _notifier.NotifyUserAsync(userId, MapDto(notification));
    }

    private static NotificationDto MapDto(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Body = n.Body,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
