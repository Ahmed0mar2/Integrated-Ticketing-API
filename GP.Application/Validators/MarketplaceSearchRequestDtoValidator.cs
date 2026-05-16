using FluentValidation;
using GP.Application.DTOs.Marketplace;

namespace GP.Application.Validators;

public class MarketplaceSearchRequestDtoValidator : AbstractValidator<MarketplaceSearchRequestDto>
{
    private const int MaxGovernorateLength = 100;

    public MarketplaceSearchRequestDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.OriginStationId)
            .GreaterThan(0)
            .WithMessage("Origin station id must be greater than 0.")
            .When(x => x.OriginStationId.HasValue);

        RuleFor(x => x.DestinationStationId)
            .GreaterThan(0)
            .WithMessage("Destination station id must be greater than 0.")
            .When(x => x.DestinationStationId.HasValue);

        RuleFor(x => x)
            .Must(x => !(x.OriginStationId.HasValue
                && x.DestinationStationId.HasValue
                && x.OriginStationId == x.DestinationStationId))
            .WithMessage("Origin and destination stations cannot be the same.");

        RuleFor(x => x.OriginGovernorate)
            .NotEmpty().WithMessage("Origin governorate cannot be empty.")
            .MaximumLength(MaxGovernorateLength).WithMessage("Origin governorate must be 100 characters or fewer.")
            .Matches(@"^[\p{L}\p{M}\p{Zs}\-'.]+$")
            .WithMessage("Origin governorate contains invalid characters.")
            .When(x => x.OriginGovernorate != null);

        RuleFor(x => x.DestinationGovernorate)
            .NotEmpty().WithMessage("Destination governorate cannot be empty.")
            .MaximumLength(MaxGovernorateLength).WithMessage("Destination governorate must be 100 characters or fewer.")
            .Matches(@"^[\p{L}\p{M}\p{Zs}\-'.]+$")
            .WithMessage("Destination governorate contains invalid characters.")
            .When(x => x.DestinationGovernorate != null);
    }
}
