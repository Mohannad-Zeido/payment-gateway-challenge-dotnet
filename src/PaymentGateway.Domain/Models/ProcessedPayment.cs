using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Models;

public record ProcessedPayment
{
    public required Guid Id { get; init; }
    public required string CardNumberLastFourDigits { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required Currency Currency { get; init; }
    public required int Amount { get; init; }
    public required PaymentStatus PaymentStatus { get; init; }
    public string? AuthorisationCode { get; init; }
}