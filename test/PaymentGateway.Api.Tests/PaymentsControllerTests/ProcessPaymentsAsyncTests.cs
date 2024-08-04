using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;


namespace PaymentGateway.Api.Tests.PaymentsControllerTests;

public class ProcessPaymentsAsyncTests : IClassFixture<WireMockServerSetup>
{
    private readonly Random _random = new();
    private WireMockServer _mockServer;

    public ProcessPaymentsAsyncTests(WireMockServerSetup wireMockServerSetup)
    {
        _mockServer = wireMockServerSetup.MockServer;
    }
    
    [Fact]
    public async Task Test()
    {
        // Arrange
        var hummus = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = 12345678909876,
            Currency = "GBP",
            Cvv = 001
        };
        
        // test case showing first digit is a 0 would be nice

        var authorizationCode = Guid.NewGuid();
        
       

        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    
                    card_number = hummus.CardNumber.ToString(),
                    expiry_date = $"{hummus.ExpiryMonth}/{hummus.ExpiryYear}",
                    currency = hummus.Currency,
                    amount = hummus.Amount,
                    cvv = hummus.Cvv.ToString()
                }))
            .RespondWith(
                Response.Create()
                    .WithStatusCode((200))
                    .WithBodyAsJson(new {
                        
                        authorized = true,
                        authorization_code = authorizationCode.ToString()
                    }));
        
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
        var paymentResponse = JsonSerializer.Deserialize<PostPaymentResponse>(await response.Content.ReadAsStringAsync());
        paymentResponse.Should().NotBeNull();

        var storedPayment = paymentsRepository.Get(paymentResponse!.Id);

        var lastFourDigits = hummus.CardNumber.Value % 10000;

        storedPayment.Should().BeEquivalentTo(new ProcessedPayment
        {
            PaymentStatus = Enum.Parse<PaymentStatus>(paymentResponse.Status),
            Amount = hummus.Amount.Value,
            Currency = Enum.Parse<Currency>(hummus.Currency),
            ExpiryMonth = hummus.ExpiryMonth.Value,
            ExpiryYear = hummus.ExpiryYear.Value,
            Id = paymentResponse.Id,
            Cvv = hummus.Cvv.Value,
            CardNumberLastFourDigits = lastFourDigits.ToString(),
            AuthorisationCode = authorizationCode.ToString()
        });
    }
    
    [Fact]
    public async Task GivenCardWithZeroInFirstDigitOfLastFourDigits_ThenResponseShouldContainContainTheZero()
    {
        // Arrange
        var hummus = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = 12345678900876,
            Currency = "GBP",
            Cvv = _random.Next(100, 9999)
        };
        
        // test case showing first digit is a 0 would be nice

        var authorizationCode = Guid.NewGuid();

        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    
                    card_number = hummus.CardNumber.ToString(),
                    expiry_date = $"{hummus.ExpiryMonth}/{hummus.ExpiryYear}",
                    currency = hummus.Currency,
                    amount = hummus.Amount,
                    cvv = hummus.Cvv.ToString()
                }))
            .RespondWith(
                Response.Create()
                    .WithStatusCode((200))
                    .WithBodyAsJson(new {
                        
                        authorized = true,
                        authorization_code = authorizationCode.ToString()
                    }));
        
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
        var paymentResponse = JsonSerializer.Deserialize<PostPaymentResponse>(await response.Content.ReadAsStringAsync());
        paymentResponse.Should().NotBeNull();

        var storedPayment = paymentsRepository.Get(paymentResponse!.Id);

        storedPayment.Should().NotBeNull();
        storedPayment!.CardNumberLastFourDigits.Should().BeEquivalentTo("0876");
    }
}