using ConsultationApi.Core.DTOs.Reviews;
using ConsultationApi.Core.Entities;
using ConsultationApi.Core.Enums;
using ConsultationApi.Core.Exceptions;
using ConsultationApi.Core.Interfaces;

namespace ConsultationApi.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public ReviewService(IUnitOfWork uow, ICurrentUserService currentUser, ICacheService cache)
    {
        _uow = uow;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<ReviewDto> SubmitReviewAsync(SubmitReviewDto request)
    {
        var patients = await _uow.Patients.FindAsync(p => p.UserId == _currentUser.UserId);
        var patient = patients.FirstOrDefault() ?? throw new NotFoundException("Patient profile not found.");

        var appointment = await _uow.Appointments.GetByIdAsync(request.AppointmentId)
            ?? throw new NotFoundException("Appointment", request.AppointmentId);

        if (appointment.PatientId != patient.Id)
            throw new ForbiddenException("Only the booking patient may submit a review.");

        if (appointment.Status != AppointmentStatus.Completed)
            throw new BusinessRuleViolationException("Reviews can only be submitted for completed appointments.");

        var existing = await _uow.Reviews.FindAsync(r => r.AppointmentId == request.AppointmentId);
        if (existing.Any())
            throw new ConflictException("A review has already been submitted for this appointment.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            AppointmentId = request.AppointmentId,
            PatientId = patient.Id,
            Rating = request.Rating,
            Comment = request.Comment
        };
        await _uow.Reviews.AddAsync(review);
        await _uow.CommitAsync();

        await RecalculateDoctorRatingAsync(appointment.DoctorId);

        return new ReviewDto
        {
            Id = review.Id,
            AppointmentId = review.AppointmentId,
            PatientName = patient.FullName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }

    private async Task RecalculateDoctorRatingAsync(Guid doctorId)
    {
        var doctor = await _uow.Doctors.GetByIdAsync(doctorId);
        if (doctor == null) return;

        var doctorAppointments = await _uow.Appointments.FindAsync(a => a.DoctorId == doctorId);
        var appointmentIds = doctorAppointments.Select(a => a.Id).ToList();

        var allReviews = new List<Review>();
        foreach (var id in appointmentIds)
        {
            var reviews = await _uow.Reviews.FindAsync(r => r.AppointmentId == id);
            allReviews.AddRange(reviews);
        }

        if (allReviews.Any())
            doctor.AverageRating = (decimal)allReviews.Average(r => r.Rating);

        await _uow.Doctors.UpdateAsync(doctor);
        await _uow.CommitAsync();

        await _cache.RemoveAsync($"doctors:{doctorId}");
        await _cache.RemoveAsync("doctors:list");
    }
}
