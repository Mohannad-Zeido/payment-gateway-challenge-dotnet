using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.Persistence;

namespace PaymentGateway.Api.Tests.PaymentsControllerTests;

public class GetPaymentAsyncTests
{
    private readonly Random _random = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() }, 
        WriteIndented = true
    };

    
    [Fact]
    public async Task GivenExistingAuthorisedPaymentId_ThenPreviouslyMadePaymentReturned()
    {
        // Arrange
        var processPaymentRequest = new ProcessPaymentRequest
        {
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 9999999999999999),
            Currency = Currency.GBP,
            Cvv = _random.Next(100, 9999).ToString()
        };

        var processedPayment = new ProcessPaymentResponse
        {
            PaymentStatus = PaymentStatus.Authorized,
            AuthorizationCode = "testing123"
        };

        var paymentsRepository = new PaymentsRepository();
        var id = await paymentsRepository.AddAsync(processPaymentRequest, processedPayment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(_jsonSerializerOptions);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paymentResponse.Should().NotBeNull();

        paymentResponse.Should().BeEquivalentTo(new GetPaymentResponse
        {
            Amount = processPaymentRequest.Amount,
            Currency = processPaymentRequest.Currency,
            Id = id,
            Status = processedPayment.PaymentStatus,
            ExpiryMonth = processPaymentRequest.ExpiryMonth,
            ExpiryYear = processPaymentRequest.ExpiryYear,
            CardNumberLastFour = GetCardLastFourDigits(processPaymentRequest.CardNumber.ToString()),
        });
    }
    
    [Fact]
    public async Task GivenExistingDeclinedPaymentId_ThenPreviouslyMadePaymentReturned()
    {
        // Arrange
        var processPaymentRequest = new ProcessPaymentRequest
        {
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 9999999999999999),
            Currency = Currency.GBP,
            Cvv = _random.Next(100, 9999).ToString()
        };

        var processedPayment = new ProcessPaymentResponse
        {
            PaymentStatus = PaymentStatus.Declined,
            AuthorizationCode = ""
        };

        var paymentsRepository = new PaymentsRepository();
        var id = await paymentsRepository.AddAsync(processPaymentRequest, processedPayment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(_jsonSerializerOptions);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        paymentResponse.Should().NotBeNull();

        paymentResponse.Should().BeEquivalentTo(new GetPaymentResponse
        {
            Amount = processPaymentRequest.Amount,
            Currency = processPaymentRequest.Currency,
            Id = id,
            Status = processedPayment.PaymentStatus,
            ExpiryMonth = processPaymentRequest.ExpiryMonth,
            ExpiryYear = processPaymentRequest.ExpiryYear,
            CardNumberLastFour = GetCardLastFourDigits(processPaymentRequest.CardNumber.ToString()),
        });
    }

    [Fact]
    public async Task GivenNonExistingPaymentId_ThenNotFoundStatusCode()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    private static string GetCardLastFourDigits(string cardNumber)
    {
        return cardNumber.Substring(cardNumber.Length - 4);
    }
}