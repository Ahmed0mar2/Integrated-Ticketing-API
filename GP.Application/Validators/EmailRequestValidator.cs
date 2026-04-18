namespace GP.Application.Validators;

using FluentValidation;
using GP.Application.DTOs.Auth;

public class EmailRequestValidator : AbstractValidator<EmailRequest>
{
    public EmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please provide a valid email address format.");
    }
}