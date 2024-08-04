using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Models;

public class ProcessPaymentResponse
{
    public required PaymentStatus PaymentStatus { get; init; }
    public string? AuthorizationCode { get; init; }
}