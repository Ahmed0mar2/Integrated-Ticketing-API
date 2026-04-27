using FluentValidation;
using GP.Application.DTOs.Bookings;

namespace GP.Application.Validators
{
    public class PassengerDetailDtoValidator : AbstractValidator<PassengerDetailDto>
    {
        public PassengerDetailDtoValidator()
        {
            RuleFor(x => x.PassengerName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.PassengerName));

            RuleFor(x => x.IdType)
                .MaximumLength(50)
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
