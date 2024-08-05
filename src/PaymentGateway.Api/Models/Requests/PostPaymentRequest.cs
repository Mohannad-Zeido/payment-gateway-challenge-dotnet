using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Requests;

public record PostPaymentRequest
{
    [JsonPropertyName("card_number")]
    public string? CardNumber { get; init; }
    [JsonPropertyName("expiry_month")]
    public int? ExpiryMonth { get; init; }
    [JsonPropertyName("expiry_year")]
    public int? ExpiryYear { get; init; }
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
    [JsonPropertyName("amount")]
    public int? Amount { get; init; }
    [JsonPropertyName("cvv")]
    public string? Cvv { get; init; }
}