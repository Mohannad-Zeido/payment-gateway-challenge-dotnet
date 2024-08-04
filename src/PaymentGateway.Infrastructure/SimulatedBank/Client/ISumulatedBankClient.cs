using PaymentGateway.Infrastructure.SimulatedBank.Models;

namespace PaymentGateway.Infrastructure.SimulatedBank;

public interface ISimulatedBankClient
{
    public Task<PaymentsResponse> PostPaymentAsync(PaymentsRequest request);
}