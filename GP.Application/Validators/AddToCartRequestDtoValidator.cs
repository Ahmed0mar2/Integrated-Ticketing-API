using FluentValidation;
using GP.Application.DTOs.Bookings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            RuleFor(x => x.Passengers)
                .NotEmpty().WithMessage("You must have at least one passenger to book a ticket.")
                .Must(p => p is { Count: <= 10 })
                .WithMessage("You can only book up to 10 seats at a time.")
                .Must(p => p.Select(x => x.IdNumber.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == p.Count)
                .WithMessage("Passenger ID numbers must be unique within a single booking.")
                .Must(p => p.Select(x => x.SeatNumber.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == p.Count)
                .WithMessage("Seat numbers must be unique within a single booking.");

            RuleForEach(x => x.Passengers).SetValidator(new PassengerDetailDtoValidator());
        }
    }
}
