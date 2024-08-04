using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.SimulatedBank.Models;

namespace PaymentGateway.Infrastructure.SimulatedBank
{
    public class SimulatedBankPaymentService : IPaymentService
    {
        private readonly ISimulatedBankClient _simulatedBankClient;

        public SimulatedBankPaymentService(ISimulatedBankClient simulatedBankClient)
        {
            _simulatedBankClient = simulatedBankClient;
        }
        public async Task<ProcessPaymentResponse> ProcessPaymentAsync(ProcessPaymentRequest request)
        {

            var response = await _simulatedBankClient.PostPaymentAsync(new PaymentsRequest
            {
                Amount = request.Amount,
                Currency = request.Currency.ToString(),
                Cvv = request.Cvv.ToString(),
                CardNumber = request.CardNumber.ToString(),
                ExpiryDate = $"{request.ExpiryMonth}/{request.ExpiryYear}",
            });

            if (response.Authorized is null)
            {
                throw new InvalidOperationException("The payment status should be set");
            }

            return new ProcessPaymentResponse
            {
                PaymentStatus = response.Authorized.Value ? PaymentStatus.Authorized : PaymentStatus.Declined,
                AuthorizationCode = response.AuthorizationCode
            };
        }
    }
}