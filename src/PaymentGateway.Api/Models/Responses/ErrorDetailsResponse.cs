using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Responses;

public class ErrorDetailsResponse
{
    [JsonPropertyName("status_code")]
    public required int StatusCode { get; set; }
    [JsonPropertyName("message")]
    public required string Message { get; set; }
}