namespace GP.Application.Validators;

using FluentValidation;
using GP.Application.DTOs.Support;

public class CreateTicketRequestDtoValidator : AbstractValidator<CreateTicketRequestDto>
{
    public CreateTicketRequestDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must be 1000 characters or fewer.");

        RuleFor(x => x.IssueCategory)
            .IsInEnum().WithMessage("Invalid issue category.");
    }
}
