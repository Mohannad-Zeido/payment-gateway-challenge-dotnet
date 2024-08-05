using System.Text.Json.Serialization;

using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Models.Responses;

public class PostPaymentResponse
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }
    [JsonPropertyName("status")]
    public required PaymentStatus Status { get; init; }
    [JsonPropertyName("card_number_last_four")]
    public required string CardNumberLastFourDigits { get; init; }
    [JsonPropertyName("expiry_month")]
    public required int ExpiryMonth { get; init; }
    [JsonPropertyName("expiry_year")]
    public required int ExpiryYear { get; init; }
    [JsonPropertyName("currency")]
    public required Currency Currency { get; init; }
    [JsonPropertyName("amount")]
    public required int Amount { get; init; }
}
