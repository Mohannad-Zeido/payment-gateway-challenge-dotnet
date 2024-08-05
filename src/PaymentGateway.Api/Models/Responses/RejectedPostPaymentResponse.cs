using System.Text.Json.Serialization;

using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Models.Responses;

public record RejectedPostPaymentResponse
{
    [JsonPropertyName("status")]
    public required PaymentStatus Status { get; init; }
    [JsonPropertyName("error_message")]
    public required string ErrorMessage { get; init; }
}