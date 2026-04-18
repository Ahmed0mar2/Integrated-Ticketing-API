using FluentValidation;
using GP.Application.DTOs.Wallet;

namespace GP.Application.Validators
{
    public class DepositRequestDtoValidator : AbstractValidator<DepositRequestDto>
    {
        public DepositRequestDtoValidator()
        {
            RuleFor(x => x.Amount)
                .InclusiveBetween(10, 10000)
                .WithMessage("You can only deposit between 10 and 10,000 EGP at a time.");

            RuleFor(x => x.MockCardNumber)
                .NotEmpty().WithMessage("Card number is required.")
                .Length(16).WithMessage("Card number must be exactly 16 digits.")
                .Matches("^\\d{16}$").WithMessage("Card number must contain only digits.");

            RuleFor(x => x.ExpiryDate)
                .NotEmpty().WithMessage("Expiry date is required.")
                .Matches("^(0[1-9]|1[0-2])\\/\\d{2}$")
                .WithMessage("Expiry date must be in MM/YY format.")
                .Must(BeAValidFutureDate).WithMessage("Card has expired."); ;

            RuleFor(x => x.Cvv)
                .NotEmpty().WithMessage("CVV is required.")
                .Length(3).WithMessage("CVV must be exactly 3 digits.")
                .Matches("^\\d{3}$").WithMessage("CVV must contain only digits.");
        }
        private bool BeAValidFutureDate(string expiryDate)
        {
            if (string.IsNullOrWhiteSpace(expiryDate) || expiryDate.Length != 5) return false;

            if (int.TryParse(expiryDate.Substring(0, 2), out int month) &&
                int.TryParse(expiryDate.Substring(3, 2), out int year))
            {
                var expiry = new DateTime(2000 + year, month, 1).AddMonths(1).AddDays(-1); 
                return expiry >= DateTime.UtcNow.Date;
            }
            return false;
        }
    }
}
