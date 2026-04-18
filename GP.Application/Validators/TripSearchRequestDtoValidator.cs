using FluentValidation;
using GP.Application.DTOs.Search;
using System;

namespace GP.Application.Validators
{
    public class TripSearchRequestDtoValidator : AbstractValidator<TripSearchRequestDto>
    {
        public TripSearchRequestDtoValidator()
        {
            // 1. Validate the Date
            RuleFor(x => x.TravelDate)
                .NotEmpty().WithMessage("Travel date is required.")
                .Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Travel date cannot be in the past.")
                .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60))
                .WithMessage("You can only search for trips up to 60 days in advance.");

            // 2. Validate Origin
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.FromGovernorate) || x.FromStationId.HasValue)
                .WithMessage("You must specify either a departure governorate or a specific departure station.")
                .WithName("Origin");

            // 3. Validate Destination
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.ToGovernorate) || x.ToStationId.HasValue)
                .WithMessage("You must specify either an arrival governorate or a specific arrival station.")
                .WithName("Destination");

            // 4. Basic Route Check
            RuleFor(x => x)
                .Must(x => !(x.FromStationId.HasValue && x.ToStationId.HasValue && x.FromStationId == x.ToStationId))
                .WithMessage("Origin and destination stations cannot be exactly the same.")
                .WithName("Route");

            // 5. Validate Passengers
            RuleFor(x => x.Passengers)
                .GreaterThan(0).WithMessage("You must search for at least 1 passenger.");

            // 6. Validate Pagination
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100.");
        }
    }
}