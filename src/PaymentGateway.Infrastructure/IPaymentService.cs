using PaymentGateway.Domain.Models;

namespace PaymentGateway.Infrastructure;

public interface IPaymentService
{
    public Task<ProcessPaymentResponse> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest);

}