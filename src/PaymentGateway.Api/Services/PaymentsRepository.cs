using PaymentGateway.Domain;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository
{
    private readonly List<ProcessedPayment> _payments = new();
    
    public Guid Add(ProcessPaymentRequest payment, ProcessPaymentResponse paymentResult)
    {
        var paymentToStore = new ProcessedPayment
        {
            Amount = payment.Amount,
            Currency = payment.Currency,
            Cvv = payment.Cvv,
            Id = Guid.NewGuid(),
            CardNumberLastFourDigits = CardNumberMasker.GetLastFourDigits(payment.CardNumber),
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            PaymentStatus = paymentResult.PaymentStatus,
            AuthorisationCode = paymentResult.AuthorizationCode
        };
            
        _payments.Add(paymentToStore);

        return paymentToStore.Id;
    }

    public ProcessedPayment? Get(Guid id)
    {
        return _payments.FirstOrDefault(p => p.Id == id);
    }
}