using ConsultationApi.Core.DTOs.Sessions;
using FluentValidation;

namespace ConsultationApi.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageDto>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content must not be empty.")
            .MaximumLength(2000).WithMessage("Message content must not exceed 2000 characters.");
    }
}
