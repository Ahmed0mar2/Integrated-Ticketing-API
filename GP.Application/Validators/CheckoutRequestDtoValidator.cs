using FluentValidation;
using GP.Application.DTOs.Bookings;

namespace GP.Application.Validators
{
    public class CheckoutRequestDtoValidator : AbstractValidator<CheckoutRequestDto>
    {
        public CheckoutRequestDtoValidator()
        {
            RuleFor(x => x.PaymentMethod)
                .NotEmpty()
                .WithMessage("Payment method is required.");
        }
    }
}
