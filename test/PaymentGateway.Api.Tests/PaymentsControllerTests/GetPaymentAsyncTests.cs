using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        var payment = new ProcessPaymentRequest
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
        var id = await paymentsRepository.AddAsync(payment, processedPayment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(_jsonSerializerOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }
    
    [Fact]
    public async Task GivenExistingDeclinedPaymentId_ThenPreviouslyMadePaymentReturned()
    {
        // Arrange
        var payment = new ProcessPaymentRequest
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
        var id = await paymentsRepository.AddAsync(payment, processedPayment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(_jsonSerializerOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
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
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}