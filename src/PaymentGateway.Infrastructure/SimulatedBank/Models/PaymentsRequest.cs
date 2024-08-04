﻿using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.SimulatedBank.Models;

public class PaymentsRequest
{
    [JsonPropertyName("card_number")]
    public required string CardNumber { get; init; }
    [JsonPropertyName("expiry_date")]
    public required string ExpiryDate { get; init; }
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }
    [JsonPropertyName("amount")]
    public required int Amount { get; init; }
    [JsonPropertyName("cvv")]
    public required string Cvv { get; init; }
}