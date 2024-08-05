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
            var expiryMonthString = request.ExpiryMonth < 10 ? "0" + request.ExpiryMonth : request.ExpiryMonth.ToString();

            var response = await _simulatedBankClient.PostPaymentAsync(new PaymentsRequest
            {
                Amount = request.Amount,
                Currency = request.Currency.ToString(),
                Cvv = request.Cvv,
                CardNumber = request.CardNumber.ToString(),
                ExpiryDate = $"{expiryMonthString}/{request.ExpiryYear}",
            });

            if (response.Authorized is null)
            {
                throw new InvalidOperationException("Authorised status should be set");
            }

            return new ProcessPaymentResponse
            {
                PaymentStatus = response.Authorized.Value ? PaymentStatus.Authorized : PaymentStatus.Declined,
                AuthorizationCode = response.AuthorizationCode
            };
        }
    }
}