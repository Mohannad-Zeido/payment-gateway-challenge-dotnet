using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Models.Responses;

public class GetPaymentResponse
{
    public required Guid Id { get; set; }
    public required PaymentStatus Status { get; set; }
    public required int CardNumberLastFour { get; set; }
    public required int ExpiryMonth { get; set; }
    public required int ExpiryYear { get; set; }
    public required Currency Currency { get; set; }
    public required int Amount { get; set; }
}