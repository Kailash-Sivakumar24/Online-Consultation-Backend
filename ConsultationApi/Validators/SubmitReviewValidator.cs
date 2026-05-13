using ConsultationApi.Core.DTOs.Reviews;
using FluentValidation;

namespace ConsultationApi.Validators;

public class SubmitReviewValidator : AbstractValidator<SubmitReviewDto>
{
    public SubmitReviewValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment != null);
    }
}
