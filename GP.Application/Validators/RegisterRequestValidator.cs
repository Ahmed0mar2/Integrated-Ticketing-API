namespace GP.Application.Validators;

using FluentValidation;
using GP.Application.DTOs.Auth;
using GP.Domain.Enums;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email too long");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .PasswordRules();

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{9,14}$").WithMessage("Invalid phone number format");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name too long");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name too long");

        RuleFor(x => x.FamilyName)
            .NotEmpty().WithMessage("Family name is required")
            .MaximumLength(100).WithMessage("Family name too long");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .Must(BeAValidAge).WithMessage("You must be at least 16 years old");

        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("IdNumber is required when IdType is provided")
            .When(x => x.IdType.HasValue);

        RuleFor(x => x.IdNumber)
            .Length(14).WithMessage("National ID must be 14 digits")
            .Matches(@"^\d+$").WithMessage("National ID must contain only digits")
            .When(x => x.IdType == IdType.NationalId);

        RuleFor(x => x.IdNumber)
            .MaximumLength(50).WithMessage("IdNumber must be 50 characters or fewer")
            .When(x => x.IdType.HasValue && x.IdType != IdType.NationalId);

        RuleFor(x => x.CountryCode)
           .NotEmpty().WithMessage("Country is required")
           .Length(2).WithMessage("Invalid country code")
           .Matches(@"^[A-Za-z]{2}$").WithMessage("Invalid country code");
    }

    private bool BeAValidAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age >= 16;
    }
}