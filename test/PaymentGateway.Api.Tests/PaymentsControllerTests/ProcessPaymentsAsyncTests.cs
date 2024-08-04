using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Application.Enums;

namespace PaymentGateway.Api.Tests.PaymentsControllerTests;

public class ProcessPaymentsAsyncTests
{
    private readonly Random _random = new();
    
    [Fact]
    public async Task Test()
    {
        // Arrange
        var hummus = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 9999999999999999),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999)
        };
        
        var paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, 
            RequestUri = new Uri("/api/Payments/", UriKind.RelativeOrAbsolute),
        };
        request.Content = new StringContent(JsonSerializer.Serialize(hummus), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        response.Should().NotBeNull();
        var paymentResponse = response.As<PostPaymentResponse>();
        paymentResponse.Should().NotBeNull();

        var storedPayment = paymentsRepository.Get(Guid.NewGuid());

        var lastFourDigits = (int) hummus.CardNumber % 10000;

        storedPayment.Should().BeEquivalentTo(new PostPaymentResponse
        {
            Status = PaymentStatus.Authorized,
            Amount = hummus.Amount,
            Currency = hummus.Currency,
            ExpiryMonth = hummus.ExpiryMonth,
            ExpiryYear = hummus.ExpiryYear,
            Id = Guid.NewGuid(),
            CardNumberLastFour = lastFourDigits,
        });
    }

}