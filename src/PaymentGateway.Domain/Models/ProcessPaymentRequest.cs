
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Models;

public record ProcessPaymentRequest
{
    public required long CardNumber { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required Currency Currency { get; init; }
    public required int Amount { get; init; }
    public required string Cvv { get; init; }
}