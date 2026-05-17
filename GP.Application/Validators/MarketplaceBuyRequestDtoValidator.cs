using FluentValidation;
using GP.Application.DTOs.Bookings;
using GP.Application.DTOs.Marketplace;

namespace GP.Application.Validators
{
    public class MarketplaceBuyRequestDtoValidator : AbstractValidator<MarketplaceBuyRequestDto>
    {
        public MarketplaceBuyRequestDtoValidator()
        {
            RuleFor(x => x.Passengers)
                .NotEmpty().WithMessage("You must provide at least one passenger.");

            RuleForEach(x => x.Passengers).SetValidator(new PassengerDetailDtoValidator());
            RuleFor(x => x.Passengers)
                .Must(HaveUniqueIds).WithMessage("Duplicate passenger IDs are not allowed in the same booking.");
        }

        private static bool HaveUniqueIds(IList<PassengerDetailDto> passengers)
        {
            if (passengers == null || passengers.Count == 0)
                return true;

            var providedIds = passengers
                .Select(p => p.IdNumber)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .ToList();

            // If the count of distinct IDs doesn't match the total count, we have a duplicate!
            return providedIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == providedIds.Count;
        }
    }
}
