using PaymentGateway.Infrastructure.SimulatedBank.Models;

namespace PaymentGateway.Infrastructure.SimulatedBank.Client;

public interface ISimulatedBankClient
{
    public Task<PaymentsResponse> PostPaymentAsync(PaymentsRequest request);
}