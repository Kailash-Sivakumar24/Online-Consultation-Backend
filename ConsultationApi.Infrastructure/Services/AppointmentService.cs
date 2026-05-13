using AutoMapper;
using ConsultationApi.Core.DTOs.Appointments;
using ConsultationApi.Core.DTOs.Common;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;

namespace ConsultationApi.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;

    public AppointmentService(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser, INotificationService notificationService)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<AppointmentDto> BookAsync(BookAppointmentDto request)
    {
        var patients = await _uow.Patients.FindAsync(p => p.UserId == _currentUser.UserId);
        var patient = patients.FirstOrDefault() ?? throw new NotFoundException("Patient profile not found.");

        var doctor = await _uow.Doctors.GetByIdAsync(request.DoctorId)
            ?? throw new NotFoundException("Doctor", request.DoctorId);

        var scheduledAtUtc = DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc);
        var appointmentEnd = scheduledAtUtc.AddMinutes(request.DurationMinutes);

        var overlapping = await _uow.Appointments.FindAsync(a =>
            a.DoctorId == request.DoctorId &&
            (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Pending) &&
            a.ScheduledAt < appointmentEnd &&
            a.ScheduledAt.AddMinutes(a.DurationMinutes) > scheduledAtUtc);

        if (overlapping.Any())
            throw new ConflictException("The doctor already has an overlapping appointment at the requested time.");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DoctorId = request.DoctorId,
            ScheduledAt = scheduledAtUtc,
            DurationMinutes = request.DurationMinutes,
            Status = AppointmentStatus.Pending
        };

        await _uow.Appointments.AddAsync(appointment);
        await _uow.CommitAsync();

        await _notificationService.CreateNotificationAsync(
            doctor.UserId, NotificationType.AppointmentBooked,
            "New Appointment", $"A new appointment has been booked for {scheduledAtUtc:g}.");

        return await MapAppointmentDtoAsync(appointment);
    }

    public async Task<PagedResult<AppointmentDto>> GetMyAppointmentsAsync(int page, int pageSize)
    {
        IEnumerable<Appointment> appointments;

        if (_currentUser.Role == UserRole.Doctor)
        {
            var doctors = await _uow.Doctors.FindAsync(d => d.UserId == _currentUser.UserId);
            var doctor = doctors.FirstOrDefault() ?? throw new NotFoundException("Doctor profile not found.");
            appointments = await _uow.Appointments.FindAsync(a => a.DoctorId == doctor.Id);
        }
        else
        {
            var patients = await _uow.Patients.FindAsync(p => p.UserId == _currentUser.UserId);
            var patient = patients.FirstOrDefault() ?? throw new NotFoundException("Patient profile not found.");
            appointments = await _uow.Appointments.FindAsync(a => a.PatientId == patient.Id);
        }

        var ordered = appointments.OrderByDescending(a => a.ScheduledAt).ToList();
        var total = ordered.Count;
        var page_data = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var dtos = new List<AppointmentDto>();
        foreach (var a in page_data)
            dtos.Add(await MapAppointmentDtoAsync(a));

        return new PagedResult<AppointmentDto>
        {
            Data = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AppointmentDto> GetByIdAsync(Guid id)
    {
        var appointment = await _uow.Appointments.GetByIdAsync(id)
            ?? throw new NotFoundException("Appointment", id);

        await EnforceOwnershipAsync(appointment);
        return await MapAppointmentDtoAsync(appointment);
    }

    public async Task<AppointmentDto> ConfirmAsync(Guid id)
    {
        var appointment = await _uow.Appointments.GetByIdAsync(id)
            ?? throw new NotFoundException("Appointment", id);

        if (_currentUser.Role != UserRole.Admin)
        {
            var doctors = await _uow.Doctors.FindAsync(d => d.UserId == _currentUser.UserId);
            var doctor = doctors.FirstOrDefault();
            if (doctor == null || doctor.Id != appointment.DoctorId)
                throw new ForbiddenException("Only the assigned doctor or admin can confirm an appointment.");
        }

        if (appointment.Status != AppointmentStatus.Pending)
            throw new BusinessRuleViolationException($"Cannot confirm appointment in '{appointment.Status}' status.");

        appointment.Status = AppointmentStatus.Confirmed;
        await _uow.Appointments.UpdateAsync(appointment);

        var session = new ConsultationSession
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointment.Id,
            SessionType = SessionType.Video
        };
        await _uow.ConsultationSessions.AddAsync(session);
        await _uow.CommitAsync();

        var patient = await _uow.Patients.GetByIdAsync(appointment.PatientId);
        if (patient != null)
        {
            await _notificationService.CreateNotificationAsync(
                patient.UserId, NotificationType.AppointmentConfirmed,
                "Appointment Confirmed", $"Your appointment on {appointment.ScheduledAt:g} has been confirmed.");
        }

        return await MapAppointmentDtoAsync(appointment);
    }

    public async Task<AppointmentDto> CancelAsync(Guid id, string? reason)
    {
        var appointment = await _uow.Appointments.GetByIdAsync(id)
            ?? throw new NotFoundException("Appointment", id);

        if (_currentUser.Role != UserRole.Admin)
        {
            var patients = await _uow.Patients.FindAsync(p => p.UserId == _currentUser.UserId);
            var patient = patients.FirstOrDefault();
            if (patient == null || patient.Id != appointment.PatientId)
                throw new ForbiddenException("Only the booking patient or admin can cancel an appointment.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new BusinessRuleViolationException("Appointment is already cancelled.");

        if (appointment.Status == AppointmentStatus.Completed)
            throw new BusinessRuleViolationException("Cannot cancel a completed appointment.");

        if ((appointment.ScheduledAt - DateTime.UtcNow).TotalHours < 1)
            throw new BusinessRuleViolationException("Cancellation is not allowed within 1 hour of the appointment.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = reason;
        await _uow.Appointments.UpdateAsync(appointment);
        await _uow.CommitAsync();

        return await MapAppointmentDtoAsync(appointment);
    }

    private async Task EnforceOwnershipAsync(Appointment appointment)
    {
        if (_currentUser.Role == UserRole.Admin) return;

        if (_currentUser.Role == UserRole.Doctor)
        {
            var doctors = await _uow.Doctors.FindAsync(d => d.UserId == _currentUser.UserId);
            if (!doctors.Any(d => d.Id == appointment.DoctorId))
                throw new ForbiddenException("Access denied.");
        }
        else
        {
            var patients = await _uow.Patients.FindAsync(p => p.UserId == _currentUser.UserId);
            if (!patients.Any(p => p.Id == appointment.PatientId))
                throw new ForbiddenException("Access denied.");
        }
    }

    private async Task<AppointmentDto> MapAppointmentDtoAsync(Appointment appointment)
    {
        var patient = await _uow.Patients.GetByIdAsync(appointment.PatientId);
        var doctor = await _uow.Doctors.GetByIdAsync(appointment.DoctorId);

        var sessions = await _uow.ConsultationSessions.FindAsync(s => s.AppointmentId == appointment.Id);
        var session = sessions.FirstOrDefault();

        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patient?.FullName ?? string.Empty,
            DoctorId = appointment.DoctorId,
            DoctorName = doctor?.FullName ?? string.Empty,
            ScheduledAt = appointment.ScheduledAt,
            DurationMinutes = appointment.DurationMinutes,
            Status = appointment.Status,
            CancellationReason = appointment.CancellationReason,
            SessionId = session?.Id,
            CreatedAt = appointment.CreatedAt
        };
    }
}
