using ConsultationApi.Core.DTOs.Prescriptions;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Infrastructure.Services;
using Moq;

namespace ConsultationApi.Tests.Unit;

public class PrescriptionServiceTests
{
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly PrescriptionService _service;

    private readonly Guid _doctorUserId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();

    public PrescriptionServiceTests()
    {
        _uow = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();

        _currentUser.Setup(c => c.UserId).Returns(_doctorUserId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Doctor);

        var doctor = new Doctor { Id = _doctorId, UserId = _doctorUserId };
        _uow.Setup(u => u.Doctors.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Doctor, bool>>>()))
            .ReturnsAsync(new List<Doctor> { doctor });

        _uow.Setup(u => u.Prescriptions.AddAsync(It.IsAny<Prescription>()))
            .ReturnsAsync((Prescription p) => p);
        _uow.Setup(u => u.MedicationItems.AddAsync(It.IsAny<MedicationItem>()))
            .ReturnsAsync((MedicationItem m) => m);
        _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        _service = new PrescriptionService(_uow.Object, _currentUser.Object);
    }

    [Fact]
    public async Task IssuePrescription_OnIncompleteSession_ThrowsBusinessRule()
    {
        var sessionId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var session = new ConsultationSession { Id = sessionId, AppointmentId = appointmentId };
        var appointment = new Appointment
        {
            Id = appointmentId,
            DoctorId = _doctorId,
            PatientId = _patientId,
            Status = AppointmentStatus.Confirmed
        };

        _uow.Setup(u => u.ConsultationSessions.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _uow.Setup(u => u.Appointments.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);

        var request = new IssuePrescriptionDto
        {
            SessionId = sessionId,
            MedicationItems = new List<MedicationItemDto> { new() { DrugName = "Aspirin", Dosage = "100mg", FrequencyPerDay = 1, DurationDays = 5 } }
        };

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.IssuePrescriptionAsync(request));
    }

    [Fact]
    public async Task IssuePrescription_DuplicateForSession_ThrowsConflict()
    {
        var sessionId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var session = new ConsultationSession { Id = sessionId, AppointmentId = appointmentId };
        var appointment = new Appointment
        {
            Id = appointmentId,
            DoctorId = _doctorId,
            PatientId = _patientId,
            Status = AppointmentStatus.Completed
        };
        var existingPrescription = new Prescription { Id = Guid.NewGuid(), SessionId = sessionId };

        _uow.Setup(u => u.ConsultationSessions.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _uow.Setup(u => u.Appointments.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
        _uow.Setup(u => u.Prescriptions.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Prescription, bool>>>()))
            .ReturnsAsync(new List<Prescription> { existingPrescription });

        var request = new IssuePrescriptionDto
        {
            SessionId = sessionId,
            MedicationItems = new List<MedicationItemDto> { new() { DrugName = "Aspirin", Dosage = "100mg", FrequencyPerDay = 1, DurationDays = 5 } }
        };

        await Assert.ThrowsAsync<ConflictException>(() => _service.IssuePrescriptionAsync(request));
    }
}
