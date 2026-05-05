namespace GP.Application.Validators;

using FluentValidation;
using GP.Application.DTOs.Marketplace;

public class ListTicketRequestDtoValidator : AbstractValidator<ListTicketRequestDto>
{
    public ListTicketRequestDtoValidator()
    {
        RuleFor(x => x.BookingId)
            .GreaterThan(0)
            .WithMessage("BookingId is required.");

        RuleFor(x => x.AskingPrice)
            .GreaterThan(0)
            .WithMessage("Asking price must be greater than zero.");
    }
}
