using Microsoft.Extensions.Logging;

using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.SimulatedBank.Client;
using PaymentGateway.Infrastructure.SimulatedBank.Models;

namespace PaymentGateway.Infrastructure.SimulatedBank
{
    public class SimulatedBankPaymentService : IPaymentService
    {
        private readonly ISimulatedBankClient _simulatedBankClient;
        private readonly ILogger<SimulatedBankPaymentService> _logger;

        public SimulatedBankPaymentService(ISimulatedBankClient simulatedBankClient, ILogger<SimulatedBankPaymentService> logger)
        {
            _simulatedBankClient = simulatedBankClient;
            _logger = logger;
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
                _logger.LogError("response did not contain authorized flag");
                throw new InvalidOperationException("Authorised status should be set");
            }

            _logger.LogInformation("Payment Response Authorized flag: '{authorized}'", response.Authorized);
            
            return new ProcessPaymentResponse
            {
                PaymentStatus = response.Authorized.Value ? PaymentStatus.Authorized : PaymentStatus.Declined,
                AuthorizationCode = response.AuthorizationCode
            };
        }
    }
}