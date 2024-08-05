using FluentValidation;

using PaymentGateway.Domain.Enums;


namespace PaymentGateway.Api.Models.Requests;

public class ProcessPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    public ProcessPaymentRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(ppr => ppr.CardNumber)
            .NotEmpty().WithMessage("CardNumber is required.")
            .Length(14, 19).WithMessage("Value must be between 14-19 characters long")
            .Must(cardNumber => long.TryParse(cardNumber, out _)).WithMessage("Must only contain numeric characters.");
        
        RuleFor(ppr => ppr.ExpiryMonth)
            .NotEmpty().WithMessage("ExpiryMonth is required.")
            .InclusiveBetween(1, 12).WithMessage("Value must be between 1-12.");
        
        RuleFor(ppr => ppr.ExpiryYear)
            .NotEmpty().WithMessage("ExpiryYear is required.")
            .Must(expiryYear => BeAValidYear(expiryYear!.Value)).WithMessage("Value must be in the future.");

        When(ppr => ppr.ExpiryMonth is not null && ppr.ExpiryYear is not null, () =>
        {
            RuleFor(x => x.ExpiryYear)
                .Must((ppr, expiryYear) => BeAValidExpiryDate(ppr.ExpiryMonth!.Value, expiryYear!.Value))
                .WithMessage("Expiry month and year combination must be in the future");
        });
            
        
        RuleFor(ppr => ppr.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be 3 characters long.")
            .Must(BeAValidCurrency).WithMessage($"Currency must be a valid ISO currency code from the following list {string.Join(",", CurrencyExtension.SupportedCurrencies())}.");

        RuleFor(ppr => ppr.Amount)
            .NotEmpty().WithMessage("Amount is required.")
            .Must(amount => amount > 0).WithMessage("Value must be greater than zero.");

        RuleFor(ppr => ppr.Cvv)
            .NotEmpty().WithMessage("CVV is required.")
            .Length(3, 4).WithMessage("Value must be 3-4 characters long.")
            .Must(x => int.TryParse(x, out _)).WithMessage("Value Must only contain numeric characters.");
    }
    
    private static bool BeAValidYear(int year)
    {
        return year >= DateTime.Now.Year;
    }
    
    private static bool BeAValidExpiryDate(int month, int year)
    {
        try
        {
            var lastDateOfExpiryMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            return lastDateOfExpiryMonth > DateTime.Now;
        }
        catch (Exception)
        {
            return false;
        }

    }

    private static bool BeAValidCurrency(string currency)
    {
        return CurrencyExtension.SupportedCurrencies().Contains(currency);
    }
}