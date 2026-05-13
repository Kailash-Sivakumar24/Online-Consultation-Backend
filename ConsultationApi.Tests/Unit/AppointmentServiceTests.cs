using ConsultationApi.Core.DTOs.Appointments;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Infrastructure.Services;
using Moq;

namespace ConsultationApi.Tests.Unit;

public class AppointmentServiceTests
{
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<AutoMapper.IMapper> _mapper;
    private readonly AppointmentService _service;

    private readonly Guid _patientUserId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _doctorUserId = Guid.NewGuid();

    public AppointmentServiceTests()
    {
        _uow = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();
        _notificationService = new Mock<INotificationService>();
        _mapper = new Mock<AutoMapper.IMapper>();

        _currentUser.Setup(c => c.UserId).Returns(_patientUserId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Patient);

        var patient = new Patient { Id = _patientId, UserId = _patientUserId, FullName = "Test Patient" };
        _uow.Setup(u => u.Patients.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Patient, bool>>>()))
            .ReturnsAsync(new List<Patient> { patient });

        var doctor = new Doctor { Id = _doctorId, UserId = _doctorUserId, FullName = "Test Doctor" };
        _uow.Setup(u => u.Doctors.GetByIdAsync(_doctorId)).ReturnsAsync(doctor);

        _uow.Setup(u => u.Appointments.AddAsync(It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment a) => a);
        _uow.Setup(u => u.Appointments.UpdateAsync(It.IsAny<Appointment>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.ConsultationSessions.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ConsultationSession, bool>>>()))
            .ReturnsAsync(new List<ConsultationSession>());
        _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        _notificationService.Setup(n => n.CreateNotificationAsync(It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _service = new AppointmentService(_uow.Object, _mapper.Object, _currentUser.Object, _notificationService.Object);
    }

    [Fact]
    public async Task BookAsync_NoOverlap_Succeeds()
    {
        _uow.Setup(u => u.Appointments.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Appointment, bool>>>()))
            .ReturnsAsync(new List<Appointment>());

        var request = new BookAppointmentDto
        {
            DoctorId = _doctorId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 30,
            SessionType = SessionType.Video
        };

        var result = await _service.BookAsync(request);

        Assert.NotNull(result);
        Assert.Equal(_doctorId, result.DoctorId);
    }

    [Fact]
    public async Task BookAsync_OverlappingAppointment_ThrowsConflict()
    {
        var scheduled = DateTime.UtcNow.AddDays(1);
        var existing = new Appointment
        {
            Id = Guid.NewGuid(),
            DoctorId = _doctorId,
            ScheduledAt = scheduled,
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed
        };

        _uow.Setup(u => u.Appointments.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Appointment, bool>>>()))
            .ReturnsAsync(new List<Appointment> { existing });

        var request = new BookAppointmentDto
        {
            DoctorId = _doctorId,
            ScheduledAt = scheduled,
            DurationMinutes = 30,
            SessionType = SessionType.Video
        };

        await Assert.ThrowsAsync<ConflictException>(() => _service.BookAsync(request));
    }

    [Fact]
    public async Task CancelAsync_WithinOneHour_ThrowsBusinessRule()
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            DoctorId = _doctorId,
            ScheduledAt = DateTime.UtcNow.AddMinutes(30),
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed
        };

        _uow.Setup(u => u.Appointments.GetByIdAsync(appointment.Id)).ReturnsAsync(appointment);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.CancelAsync(appointment.Id, null));
    }

    [Fact]
    public async Task CancelAsync_UnauthorizedUser_ThrowsForbidden()
    {
        var otherPatientId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = otherPatientId,
            DoctorId = _doctorId,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed
        };

        _uow.Setup(u => u.Appointments.GetByIdAsync(appointment.Id)).ReturnsAsync(appointment);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.CancelAsync(appointment.Id, null));
    }

    [Fact]
    public async Task CancelAsync_ValidCancellation_Succeeds()
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = _patientId,
            DoctorId = _doctorId,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            DurationMinutes = 30,
            Status = AppointmentStatus.Confirmed
        };

        _uow.Setup(u => u.Appointments.GetByIdAsync(appointment.Id)).ReturnsAsync(appointment);

        var result = await _service.CancelAsync(appointment.Id, "Changed plans");

        Assert.Equal(AppointmentStatus.Cancelled, result.Status);
        Assert.Equal("Changed plans", result.CancellationReason);
    }
}
