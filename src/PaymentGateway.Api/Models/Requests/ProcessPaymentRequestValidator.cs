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
            .Must(cardNumber => BeValidCardNumber(cardNumber!.Value)).WithMessage("Value must be between 14-19 characters long");
        
        RuleFor(ppr => ppr.ExpiryMonth)
            .NotEmpty().WithMessage("ExpiryMonth is required.")
            .InclusiveBetween(1, 12).WithMessage("Value must be between 1-12.");
        
        RuleFor(card => card.ExpiryYear)
            .NotEmpty().WithMessage("ExpiryYear is required.")
            .Must(expiryYear => BeAValidYear(expiryYear!.Value)).WithMessage("Value must be in the future")
            .Must((card, expiryYear) => BeAValidExpiryDate(card.ExpiryMonth!.Value, expiryYear!.Value)).WithMessage("Expiry month and year combination must be in the future");
        
        RuleFor(card => card.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be 3 characters long.")
            .Must(BeAValidCurrency).WithMessage("Currency must be a valid ISO currency code.");

        RuleFor(card => card.Amount)
            .NotEmpty().WithMessage("Amount is required")
            .Must(amount => amount > 0).WithMessage("Amount must be greater than zero.");

        RuleFor(ppr => ppr.Cvv)
            .NotEmpty().WithMessage("CVV is required.")
            .Length(3, 4).WithMessage("CVV must be 3-4 characters long.")
            .Must(x => int.TryParse(x, out _)).WithMessage("Must only contain numeric characters.");
    }
    
    private static bool BeValidCardNumber(long number)
    {
        var length =  number.ToString().Length;
        return length is >= 14 and <= 19;
    }
    
    private static bool BeAValidYear(int year)
    {
        return year >= DateTime.Now.Year;
    }
    
    private static bool BeAValidExpiryDate(int month, int year)
    {
        var lastDateOfExpiryMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return lastDateOfExpiryMonth > DateTime.Now;
    }

    private static bool BeAValidCurrency(string currency)
    {
        return CurrencyExtension.SupportedCurrencies().Contains(currency);
    }
}