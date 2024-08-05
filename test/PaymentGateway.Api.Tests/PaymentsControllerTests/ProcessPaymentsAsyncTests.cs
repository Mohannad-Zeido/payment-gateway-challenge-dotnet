using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _sut;
    private readonly PaymentsRepository _paymentsRepository;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() }, 
        WriteIndented = true
    };

    public ProcessPaymentsAsyncTests(WireMockServerSetup wireMockServerSetup)
    {
        _mockServer = wireMockServerSetup.MockServer;
        
        _paymentsRepository = new PaymentsRepository();
        
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        _sut = webApplicationFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => ((ServiceCollection)services)
                    .AddSingleton(_paymentsRepository)))
            .CreateClient();
        
        
    }
    
    [Fact]
    public async Task GivenPaymentRequestIsValid_ThenResponseShouldBeAuthorisedPayment_AndPaymentSaved()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 9999999999999999),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
        };

        var expectedAuthorisationCode = Guid.NewGuid();
        
        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    
                    card_number = payment.CardNumber.ToString(),
                    expiry_date = $"{payment.ExpiryMonth}/{payment.ExpiryYear}",
                    currency = payment.Currency,
                    amount = payment.Amount,
                    cvv = payment.Cvv.ToString()
                }))
            .RespondWith(
                Response.Create()
                    .WithStatusCode((200))
                    .WithBodyAsJson(new {
                        
                        authorized = true,
                        authorization_code = expectedAuthorisationCode.ToString()
                    }));
        
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, 
            RequestUri = new Uri("/api/Payments/", UriKind.RelativeOrAbsolute),
            Content = new StringContent(JsonSerializer.Serialize(payment, _jsonSerializerOptions), Encoding.UTF8, "application/json")
        };

        var response = await _sut.SendAsync(request);

        response.EnsureSuccessStatusCode();

        response.Should().NotBeNull();
        var paymentResponse = JsonSerializer.Deserialize<PostPaymentResponse>(await response.Content.ReadAsStringAsync(), _jsonSerializerOptions);
        paymentResponse.Should().NotBeNull();

        var storedPayment = _paymentsRepository.Get(paymentResponse!.Id);

        //TODO change the cardMasking logic
        var lastFourDigits = payment.CardNumber.Value % 10000;

        storedPayment.Should().BeEquivalentTo(new ProcessedPayment
        {
            PaymentStatus = PaymentStatus.Authorized,
            Amount = payment.Amount.Value,
            Currency = Enum.Parse<Currency>(payment.Currency),
            ExpiryMonth = payment.ExpiryMonth.Value,
            ExpiryYear = payment.ExpiryYear.Value,
            Id = paymentResponse.Id,
            CardNumberLastFourDigits = lastFourDigits.ToString(),
            AuthorisationCode = expectedAuthorisationCode.ToString()
        });
    }
    
    [Theory]
    [InlineData("01", "Cvv: CVV must be 3-4 characters long.")]
    [InlineData("", "Cvv: CVV is required.")]
    [InlineData(" ", "Cvv: CVV is required.")]
    [InlineData(null, "Cvv: CVV is required.")]
    [InlineData("\n", "Cvv: CVV is required.")]
    [InlineData("0f6", "Cvv: CVV must only contain numeric characters.")]
    public async Task GivenCvvIsNotValidFormat_ThenResponseShouldBeBadRequestWithCvvValidationError(string cvv, string expectedErrorMessage)
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 9999999999999999),
            Currency = "GBP",
            Cvv = cvv
        };
        
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, 
            RequestUri = new Uri("/api/Payments/", UriKind.RelativeOrAbsolute),
            Content = new StringContent(JsonSerializer.Serialize(payment, _jsonSerializerOptions), Encoding.UTF8, "application/json")
        };
        
        var response = await _sut.SendAsync(request);
        
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var rejectedPostPaymentResponse = JsonSerializer.Deserialize<RejectedPostPaymentResponse>(await response.Content.ReadAsStringAsync(), _jsonSerializerOptions);
        
        rejectedPostPaymentResponse.Should().NotBeNull();

        rejectedPostPaymentResponse.Should().BeEquivalentTo(new RejectedPostPaymentResponse
        {
            Status = PaymentStatus.Rejected, 
            ErrorMessage = expectedErrorMessage,
        });

    }
    
    [Fact]
    public async Task GivenCvvStartsWithZero_ThenRequestToAcquirerShouldContainUnchangedCvv()
    {
        // Arrange
        const string expectedCvv = "005";
        var paymentRequest = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = 12345678900876,
            Currency = "GBP",
            Cvv = expectedCvv
        };

        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    
                    card_number = paymentRequest.CardNumber.ToString(),
                    expiry_date = $"{paymentRequest.ExpiryMonth}/{paymentRequest.ExpiryYear}",
                    currency = paymentRequest.Currency,
                    amount = paymentRequest.Amount,
                    cvv = expectedCvv
                }))
            .RespondWith(
                Response.Create()
                    .WithStatusCode((200))
                    .WithBodyAsJson(new {
                        
                        authorized = true,
                        authorization_code = Guid.NewGuid().ToString()
                    }));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, 
            RequestUri = new Uri("/api/Payments/", UriKind.RelativeOrAbsolute),
            Content = new StringContent(JsonSerializer.Serialize(paymentRequest, _jsonSerializerOptions), Encoding.UTF8, "application/json")
        };

        // Act
        var response = await _sut.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        _mockServer.LogEntries.Count().Should().Be(1);

    }
    
    [Fact]
    public async Task GivenCardWithZeroInFirstDigitOfLastFourDigits_ThenResponseShouldContainContainTheZero()
    {
        // Arrange
        var paymentRequest = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = 12345678900876,
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
        };

        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    card_number = paymentRequest.CardNumber.ToString(),
                    expiry_date = $"{paymentRequest.ExpiryMonth}/{paymentRequest.ExpiryYear}",
                    currency = paymentRequest.Currency,
                    amount = paymentRequest.Amount,
                    cvv = paymentRequest.Cvv.ToString()
                }))
            .RespondWith(
                Response.Create()
                    .WithStatusCode((200))
                    .WithBodyAsJson(new {
                        
                        authorized = true,
                        authorization_code = Guid.NewGuid().ToString()
                    }));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, 
            RequestUri = new Uri("/api/Payments/", UriKind.RelativeOrAbsolute),
            Content = new StringContent(JsonSerializer.Serialize(paymentRequest, _jsonSerializerOptions), Encoding.UTF8, "application/json")
        };

        // Act
        var response = await _sut.SendAsync(request);
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var paymentResponse = JsonSerializer.Deserialize<PostPaymentResponse>(await response.Content.ReadAsStringAsync(), _jsonSerializerOptions );
        paymentResponse.Should().NotBeNull();
        
        paymentResponse!.CardNumberLastFour.Should().BeEquivalentTo("0876");
    }
}