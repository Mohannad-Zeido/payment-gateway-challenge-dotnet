using PaymentGateway.Domain;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Infrastructure.Persistence;

public class PaymentsRepository
{
    private readonly List<ProcessedPayment> _payments = new();
    
    public Task<Guid> AddAsync(ProcessPaymentRequest payment, ProcessPaymentResponse paymentResult)
    {
        var paymentToStore = new ProcessedPayment
        {
            Amount = payment.Amount,
            Currency = payment.Currency,
            Id = Guid.NewGuid(),
            CardNumberLastFourDigits = CardNumberMasker.GetLastFourDigits(payment.CardNumber),
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            PaymentStatus = paymentResult.PaymentStatus,
            AuthorisationCode = paymentResult.AuthorizationCode
        };
            
        _payments.Add(paymentToStore);

        return Task.FromResult(paymentToStore.Id);
    }

    public Task<ProcessedPayment?> GetAsync(Guid id)
    {
        return Task.FromResult(_payments.FirstOrDefault(p => p.Id == id));
    }
}