using FluentValidation;
using GP.Application.DTOs.Profile;

namespace GP.Application.Validators;

public class FcmTokenRequestDtoValidator : AbstractValidator<FcmTokenRequestDto>
{
    public FcmTokenRequestDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(512).WithMessage("Token must be 512 characters or fewer.");

        RuleFor(x => x.DeviceType)
            .NotEmpty().WithMessage("Device type is required.")
            .MaximumLength(50).WithMessage("Device type must be 50 characters or fewer.");
    }
}
