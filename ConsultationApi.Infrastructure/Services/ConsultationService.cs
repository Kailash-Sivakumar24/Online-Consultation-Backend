using ConsultationApi.Core.DTOs.Sessions;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Core.Entities;

namespace ConsultationApi.Infrastructure.Services;

public class ConsultationService : IConsultationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ISignalRNotifier _notifier;

    public ConsultationService(IUnitOfWork uow, ICurrentUserService currentUser, ISignalRNotifier notifier)
    {
        _uow = uow;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<SessionDto> StartSessionAsync(Guid sessionId)
    {
        var session = await _uow.ConsultationSessions.GetByIdAsync(sessionId)
            ?? throw new NotFoundException("ConsultationSession", sessionId);

        var appointment = await _uow.Appointments.GetByIdAsync(session.AppointmentId)
            ?? throw new NotFoundException("Appointment", session.AppointmentId);

        if (appointment.Status != AppointmentStatus.Confirmed)
            throw new BusinessRuleViolationException("Appointment must be confirmed before starting a session.");

        var doctors = await _uow.Doctors.FindAsync(d => d.UserId == _currentUser.UserId);
        var doctor = doctors.FirstOrDefault();
        if (doctor == null || doctor.Id != appointment.DoctorId)
            throw new ForbiddenException("Only the assigned doctor can start a session.");

        session.StartedAt = DateTime.UtcNow;
        session.RoomUrl = $"https://consultation.room/{sessionId}";
        await _uow.ConsultationSessions.UpdateAsync(session);
        await _uow.CommitAsync();

        await _notifier.NotifySessionStatusChangedAsync(sessionId, "Started");
        return MapSessionDto(session);
    }

    public async Task<SessionDto> EndSessionAsync(Guid sessionId)
    {
        var session = await _uow.ConsultationSessions.GetByIdAsync(sessionId)
            ?? throw new NotFoundException("ConsultationSession", sessionId);

        var appointment = await _uow.Appointments.GetByIdAsync(session.AppointmentId)
            ?? throw new NotFoundException("Appointment", session.AppointmentId);

        var doctors = await _uow.Doctors.FindAsync(d => d.UserId == _currentUser.UserId);
        var doctor = doctors.FirstOrDefault();
        if (doctor == null || doctor.Id != appointment.DoctorId)
            throw new ForbiddenException("Only the assigned doctor can end a session.");

        session.EndedAt = DateTime.UtcNow;
        await _uow.ConsultationSessions.UpdateAsync(session);

        appointment.Status = AppointmentStatus.Completed;
        await _uow.Appointments.UpdateAsync(appointment);
        await _uow.CommitAsync();

        await _notifier.NotifySessionStatusChangedAsync(sessionId, "Ended");
        return MapSessionDto(session);
    }

    public async Task<ChatMessageDto> AddMessageAsync(Guid sessionId, string content)
    {
        var session = await _uow.ConsultationSessions.GetByIdAsync(sessionId)
            ?? throw new NotFoundException("ConsultationSession", sessionId);

        if (session.StartedAt == null || session.EndedAt != null)
            throw new BusinessRuleViolationException("Session is not active.");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SenderId = _currentUser.UserId,
            Content = content
        };
        await _uow.ChatMessages.AddAsync(message);
        await _uow.CommitAsync();

        var dto = new ChatMessageDto
        {
            Id = message.Id,
            SessionId = sessionId,
            SenderId = _currentUser.UserId,
            SenderName = _currentUser.Email,
            Content = content,
            CreatedAt = message.CreatedAt
        };

        await _notifier.BroadcastChatMessageAsync(sessionId, dto);
        return dto;
    }

    public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid sessionId)
    {
        var session = await _uow.ConsultationSessions.GetByIdAsync(sessionId)
            ?? throw new NotFoundException("ConsultationSession", sessionId);

        var messages = await _uow.ChatMessages.FindAsync(m => m.SessionId == sessionId);
        return messages.OrderBy(m => m.CreatedAt).Select(m => new ChatMessageDto
        {
            Id = m.Id,
            SessionId = m.SessionId,
            SenderId = m.SenderId,
            Content = m.Content,
            CreatedAt = m.CreatedAt
        });
    }

    private static SessionDto MapSessionDto(ConsultationSession session) => new()
    {
        Id = session.Id,
        AppointmentId = session.AppointmentId,
        SessionType = session.SessionType,
        RoomUrl = session.RoomUrl,
        StartedAt = session.StartedAt,
        EndedAt = session.EndedAt,
        Notes = session.Notes
    };
}
