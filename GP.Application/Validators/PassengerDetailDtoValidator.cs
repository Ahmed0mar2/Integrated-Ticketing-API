using FluentValidation;
using GP.Application.DTOs.Bookings;

namespace GP.Application.Validators
{
    public class PassengerDetailDtoValidator : AbstractValidator<PassengerDetailDto>
    {
        public PassengerDetailDtoValidator()
        {
            RuleFor(x => x.PassengerName)
                .NotEmpty().WithMessage("Passenger name is required.")
                .MaximumLength(200).WithMessage("Passenger name must be 200 characters or fewer.");

            RuleFor(x => x.IdType)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.IdType));

            RuleFor(x => x.IdNumber)
                .NotEmpty().WithMessage("ID number is required when ID type is provided.")
                .When(x => !string.IsNullOrWhiteSpace(x.IdType));

            RuleFor(x => x.IdNumber)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.IdNumber));

            RuleFor(x => x.SeatNumber)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.SeatNumber));
        }
    }
}
