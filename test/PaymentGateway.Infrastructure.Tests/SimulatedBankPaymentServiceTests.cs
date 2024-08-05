using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.SimulatedBank;
using PaymentGateway.Infrastructure.SimulatedBank.Client;
using PaymentGateway.Infrastructure.SimulatedBank.Models;

namespace PaymentGateway.Infrastructure.Tests;

public class SimulatedBankPaymentServiceTests
{
    [Theory]
    [InlineData(1, "01")]
    [InlineData(2, "02")]
    [InlineData(3, "03")]
    [InlineData(4, "04")]
    [InlineData(5, "05")]
    [InlineData(6, "06")]
    [InlineData(7, "07")]
    [InlineData(8, "08")]
    [InlineData(9, "09")]
    [InlineData(10, "10")]
    [InlineData(11, "11")]
    [InlineData(12, "12")]
    public async Task Test1(int month, string expectedMonthRepresentation)
    {
        var fixture = new Fixture();
        var mockClient = Substitute.For<ISimulatedBankClient>();

        PaymentsRequest? capturedPaymentRequest = null;
        mockClient.PostPaymentAsync(Arg.Do<PaymentsRequest>(pr => capturedPaymentRequest = pr))
            .Returns(
            new PaymentsResponse
            {
                Authorized = true, 
                AuthorizationCode = fixture.Create<string>(),
            });
        var request = new ProcessPaymentRequest
        {
            Amount = fixture.Create<int>(),
            Currency = fixture.Create<Currency>(),
            Cvv = fixture.Create<string>(),
            CardNumber = fixture.Create<long>(),
            ExpiryMonth = month,
            ExpiryYear = fixture.Create<DateTime>().Year,
        };
        
        var sut = new SimulatedBankPaymentService(mockClient, NullLogger<SimulatedBankPaymentService>.Instance);

        var result = await sut.ProcessPaymentAsync(request);
        
        // Assert
        result.Should().NotBeNull();
        capturedPaymentRequest.Should().NotBeNull();
        capturedPaymentRequest!.ExpiryDate.Should().Be($"{expectedMonthRepresentation}/{request.ExpiryYear}");
    }
}