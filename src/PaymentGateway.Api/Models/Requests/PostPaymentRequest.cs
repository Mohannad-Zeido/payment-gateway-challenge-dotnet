namespace PaymentGateway.Api.Models.Requests;

public record PostPaymentRequest
{
    public long? CardNumber { get; init; }
    public int? ExpiryMonth { get; init; }
    public int? ExpiryYear { get; init; }
    public string? Currency { get; init; }
    public int? Amount { get; init; }
    public int? Cvv { get; init; }
}