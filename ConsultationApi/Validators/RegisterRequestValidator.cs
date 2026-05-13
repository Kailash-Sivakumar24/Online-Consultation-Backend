using ConsultationApi.Core.DTOs.Auth;
using ConsultationApi.Core.Enums;
using FluentValidation;

namespace ConsultationApi.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).IsInEnum();

        When(x => x.Role == UserRole.Doctor, () =>
        {
            RuleFor(x => x.Specialization).NotEmpty();
            RuleFor(x => x.LicenseNumber).NotEmpty();
            RuleFor(x => x.ConsultationFee).NotNull().GreaterThan(0)
                .WithMessage("Consultation fee must be greater than 0.");
        });

        When(x => x.Role == UserRole.Patient, () =>
        {
            RuleFor(x => x.DateOfBirth).NotNull();
            RuleFor(x => x.Gender).NotNull();
        });
    }
}
