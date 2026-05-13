using ConsultationApi.Core.DTOs.Appointments;
using FluentValidation;

namespace ConsultationApi.Validators;

public class BookAppointmentValidator : AbstractValidator<BookAppointmentDto>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be scheduled in the future.");
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 120)
            .WithMessage("Duration must be between 15 and 120 minutes.");
        RuleFor(x => x.SessionType).IsInEnum();
    }
}
