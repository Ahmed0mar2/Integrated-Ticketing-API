using FluentValidation;
using GP.Application.DTOs.Profile;

namespace GP.Application.Validators;

public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must be 50 characters or fewer.");

        RuleFor(x => x.FamilyName)
            .NotEmpty().WithMessage("Family name is required.")
            .MaximumLength(50).WithMessage("Family name must be 50 characters or fewer.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must be 50 characters or fewer.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must be 100 characters or fewer.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must be 20 characters or fewer.")
            .When(x => x.PhoneNumber != null);
    }
}
