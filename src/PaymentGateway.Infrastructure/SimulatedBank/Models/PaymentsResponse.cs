using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.SimulatedBank.Models;

public class PaymentsResponse
{
    [JsonPropertyName("authorized")]
    public bool? Authorized { get; init; }
    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; init; }
}