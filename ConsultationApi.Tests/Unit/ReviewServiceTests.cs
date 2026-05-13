using ConsultationApi.Core.DTOs.Reviews;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;
using ConsultationApi.Infrastructure.Services;
using Moq;

namespace ConsultationApi.Tests.Unit;

public class ReviewServiceTests
{
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly Mock<ICacheService> _cache;
    private readonly ReviewService _service;

    private readonly Guid _patientUserId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();

    public ReviewServiceTests()
    {
        _uow = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();
        _cache = new Mock<ICacheService>();

        _currentUser.Setup(c => c.UserId).Returns(_patientUserId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Patient);

        var patient = new Patient { Id = _patientId, UserId = _patientUserId, FullName = "Test Patient" };
        _uow.Setup(u => u.Patients.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Patient, bool>>>()))
            .ReturnsAsync(new List<Patient> { patient });

        _uow.Setup(u => u.Reviews.AddAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) => r);
        _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        _cache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        _service = new ReviewService(_uow.Object, _currentUser.Object, _cache.Object);
    }

    [Fact]
    public async Task SubmitReview_DuplicateReview_ThrowsConflict()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = appointmentId,
            PatientId = _patientId,
            DoctorId = _doctorId,
            Status = AppointmentStatus.Completed
        };
        var existingReview = new Review { Id = Guid.NewGuid(), AppointmentId = appointmentId };

        _uow.Setup(u => u.Appointments.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
        _uow.Setup(u => u.Reviews.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
            .ReturnsAsync(new List<Review> { existingReview });

        var request = new SubmitReviewDto { AppointmentId = appointmentId, Rating = 5 };

        await Assert.ThrowsAsync<ConflictException>(() => _service.SubmitReviewAsync(request));
    }

    [Fact]
    public async Task SubmitReview_RecalculatesAverageRating()
    {
        var appointmentId = Guid.NewGuid();
        var appointment1Id = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = appointmentId,
            PatientId = _patientId,
            DoctorId = _doctorId,
            Status = AppointmentStatus.Completed
        };
        var doctor = new Doctor { Id = _doctorId, AverageRating = 4.0m };

        _uow.Setup(u => u.Appointments.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
        _uow.Setup(u => u.Reviews.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
            .ReturnsAsync(new List<Review>());
        _uow.Setup(u => u.Doctors.GetByIdAsync(_doctorId)).ReturnsAsync(doctor);
        _uow.Setup(u => u.Appointments.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Appointment, bool>>>()))
            .ReturnsAsync(new List<Appointment> { appointment });

        _uow.SetupSequence(u => u.Reviews.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
            .ReturnsAsync(new List<Review>())
            .ReturnsAsync(new List<Review>
            {
                new Review { AppointmentId = appointmentId, Rating = 5 }
            });

        _uow.Setup(u => u.Doctors.UpdateAsync(It.IsAny<Doctor>())).Returns(Task.CompletedTask);

        var request = new SubmitReviewDto { AppointmentId = appointmentId, Rating = 5 };
        var result = await _service.SubmitReviewAsync(request);

        Assert.Equal(5, result.Rating);
        _uow.Verify(u => u.Doctors.UpdateAsync(It.Is<Doctor>(d => d.AverageRating > 0)), Times.Once);
    }
}
