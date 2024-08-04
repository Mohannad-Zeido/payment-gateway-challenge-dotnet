using PaymentGateway.Application.Enums;

namespace PaymentGateway.Application.Models;

public record ProcessPaymentRequest
{
    public long CardNumber { get; init; }
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public Currency Currency { get; init; }
    public int Amount { get; init; }
    public int Cvv { get; init; }
}