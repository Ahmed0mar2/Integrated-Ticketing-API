using FluentValidation;
using GP.Application.DTOs.Bookings;

namespace GP.Application.Validators
{
    public class PassengerDetailDtoValidator : AbstractValidator<PassengerDetailDto>
    {
        public PassengerDetailDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Passenger name is required.")
                .MaximumLength(200);

            RuleFor(x => x.Age)
                .InclusiveBetween(1, 120)
                .WithMessage("Passenger age must be between 1 and 120.");

            RuleFor(x => x.IdType)
                .IsInEnum().WithMessage("Invalid passenger ID type. Allowed values: NationalId, Passport, DrivingLicense, StudentId, Other.");

            RuleFor(x => x.IdNumber)
                .NotEmpty().WithMessage("Passenger ID/Passport number is required.")
                .MaximumLength(50);

            RuleFor(x => x.SeatNumber)
                .NotEmpty().WithMessage("Seat number is required.")
                .MaximumLength(50);
        }
    }
}
