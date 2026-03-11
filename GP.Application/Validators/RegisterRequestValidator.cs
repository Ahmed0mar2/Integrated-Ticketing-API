namespace GP.Application.Validators;

using FluentValidation;
using GP.Application.DTOs.Auth;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private readonly ApplicationDbContext _context;
    public RegisterRequestValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email too long");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit");

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

        RuleFor(x => x.NationalIdNumber)
            .Length(14).WithMessage("National ID must be 14 digits")
            .Matches(@"^\d+$").WithMessage("National ID must contain only digits")
            .When(x => !string.IsNullOrWhiteSpace(x.NationalIdNumber));

        RuleFor(x => x.CountryCode)
           .NotEmpty().WithMessage("Country is required")
           .Length(2).WithMessage("Invalid country code")
           .MustAsync(BeAValidCountryCode).WithMessage("Invalid country");
    }

    private async Task<bool> BeAValidCountryCode(string countryCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return false;
        var code = countryCode.ToUpperInvariant();
        return await _context.Countries.AnyAsync(c => c.CountryCode == code, cancellationToken);
    }

    private bool BeAValidAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age >= 16;
    }
}