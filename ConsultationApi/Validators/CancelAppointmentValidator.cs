using ConsultationApi.Core.DTOs.Appointments;
using FluentValidation;

namespace ConsultationApi.Validators;

public class CancelAppointmentValidator : AbstractValidator<CancelAppointmentDto>
{
    public CancelAppointmentValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).When(x => x.Reason != null)
            .WithMessage("Cancellation reason must not exceed 500 characters.");
    }
}
