using FluentValidation;

using PaymentGateway.Application.Enums;

namespace PaymentGateway.Api.Models.Requests;

public class ProcessPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    public ProcessPaymentRequestValidator()
    {
        RuleFor(ppr => ppr.CardNumber)
            .NotEmpty()
            .Must(cardNumber => BeValidCardNumber(cardNumber!.Value));
        
        RuleFor(ppr => ppr.ExpiryMonth)
            .NotEmpty()
            .InclusiveBetween(1, 12);
        
        RuleFor(card => card.ExpiryYear)
            .NotEmpty()
            .Must((card, expiryYear) => BeAValidExpiryDate(card.ExpiryMonth!.Value, expiryYear!.Value));
        
        RuleFor(card => card.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be 3 characters long.")
            .Must(BeAValidCurrency).WithMessage("Currency must be a valid ISO currency code.");

        RuleFor(card => card.Amount)
            .Must(amount => amount > 0).WithMessage("Amount must be greater than zero.");
        
        RuleFor(ppr => ppr.Cvv)
            .NotEmpty()
            .Must(cvv => BeValidCvv(cvv!.Value));

    }
    
    private static bool BeValidCardNumber(long number)
    {
        var length =  number.ToString().Length;
        return length is >= 14 and <= 16;
    }
    
    private static bool BeValidCvv(int number)
    {
        var length =  number.ToString().Length;
        return length is >= 3 and <= 4;
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