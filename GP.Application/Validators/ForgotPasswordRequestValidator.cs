namespace GP.Application.Validators;

using FluentValidation;
using GP.Application.DTOs.Auth;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please provide a valid email address format.");
    }
}