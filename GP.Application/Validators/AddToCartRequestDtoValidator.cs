using FluentValidation;
using GP.Application.DTOs.Bookings;

namespace GP.Application.Validators
{
    public class AddToCartRequestDtoValidator : AbstractValidator<AddToCartRequestDto>
    {
        public AddToCartRequestDtoValidator()
        {
            RuleFor(x => x.TripOccurrenceId).GreaterThan(0).WithMessage("Invalid Trip Occurrence.");
            RuleFor(x => x.CoachClassId).GreaterThan(0).WithMessage("Invalid Coach Class.");
            RuleFor(x => x.OriginStationId).GreaterThan(0).WithMessage("Invalid Origin Station.");
            RuleFor(x => x.DestinationStationId).GreaterThan(0).WithMessage("Invalid Destination Station.");

            RuleFor(x => x)
                .Must(x => x.OriginStationId != x.DestinationStationId)
                .WithMessage("Origin and destination stations cannot be the same.");

            RuleFor(x => x.ContactName)
                .NotEmpty().WithMessage("Contact name is required.")
                .MaximumLength(200);

            RuleFor(x => x.ContactPhone)
                .NotEmpty().WithMessage("Contact phone is required.")
                .MaximumLength(50);

            RuleFor(x => x.ContactEmail)
                .NotEmpty().WithMessage("Contact email is required.")
                .EmailAddress().WithMessage("A valid contact email is required.")
                .MaximumLength(255);

            RuleFor(x => x.Passengers)
                .NotEmpty().WithMessage("You must have at least one passenger to book a ticket.")
                .Must(p => p is { Count: <= 10 })
                .WithMessage("You can only book up to 10 seats at a time.");

            RuleForEach(x => x.Passengers).SetValidator(new PassengerDetailDtoValidator());
        }
    }
}
