using System.Text.Json.Serialization;

using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Models.Responses;

public class GetPaymentResponse
{
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }
    
    [JsonPropertyName("status")]
    public required PaymentStatus Status { get; set; }
    
    [JsonPropertyName("card_number_last_four")]
    public required string CardNumberLastFour { get; set; }
    [JsonPropertyName("expiry_month")]
    public required int ExpiryMonth { get; set; }
    [JsonPropertyName("expiry_year")]
    public required int ExpiryYear { get; set; }
    [JsonPropertyName("currency")]
    public required Currency Currency { get; set; }
    [JsonPropertyName("amount")]
    public required int Amount { get; set; }
}