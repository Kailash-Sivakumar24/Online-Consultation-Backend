using ConsultationApi.Core.DTOs.Prescriptions;
using FluentValidation;

namespace ConsultationApi.Validators;

public class IssuePrescriptionValidator : AbstractValidator<IssuePrescriptionDto>
{
    public IssuePrescriptionValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.MedicationItems).NotEmpty()
            .WithMessage("At least one medication item is required.");
        RuleForEach(x => x.MedicationItems).SetValidator(new MedicationItemValidator());
    }
}

public class MedicationItemValidator : AbstractValidator<MedicationItemDto>
{
    public MedicationItemValidator()
    {
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FrequencyPerDay).GreaterThan(0);
        RuleFor(x => x.DurationDays).GreaterThan(0);
    }
}
