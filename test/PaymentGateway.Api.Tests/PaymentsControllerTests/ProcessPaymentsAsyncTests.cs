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
    public async Task GivenPaymentRequestIsValid_ThenResponseShouldBeSuccessWithAuthorisedPayment_AndPaymentSaved()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
        };

        var expectedAuthorisationCode = Guid.NewGuid();
        
        _mockServer.Reset();
        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    
                    card_number = payment.CardNumber,
                    expiry_date = $"{payment.ExpiryMonth}/{payment.ExpiryYear}",
                    currency = payment.Currency,
                    amount = payment.Amount,
                    cvv = payment.Cvv
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
        

        storedPayment.Should().BeEquivalentTo(new ProcessedPayment
        {
            PaymentStatus = PaymentStatus.Authorized,
            Amount = payment.Amount.Value,
            Currency = Enum.Parse<Currency>(payment.Currency),
            ExpiryMonth = payment.ExpiryMonth.Value,
            ExpiryYear = payment.ExpiryYear.Value,
            Id = paymentResponse.Id,
            CardNumberLastFourDigits = GetCardLastFourDigits(payment.CardNumber),
            AuthorisationCode = expectedAuthorisationCode.ToString()
        });
    }
    
    [Fact]
    public async Task GivenPaymentRequestIsValid_WhenAcquirerBankDeclinesPayment_ThenResponseShouldBeSuccessWithDeclinedPayment_AndPaymentSaved()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
        };
        
        _mockServer.Reset();
        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    
                    card_number = payment.CardNumber,
                    expiry_date = $"{payment.ExpiryMonth}/{payment.ExpiryYear}",
                    currency = payment.Currency,
                    amount = payment.Amount,
                    cvv = payment.Cvv
                }))
            .RespondWith(
                Response.Create()
                    .WithStatusCode((200))
                    .WithBodyAsJson(new {
                        
                        authorized = false,
                        authorization_code = ""
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

        paymentResponse.Should().BeEquivalentTo(new PostPaymentResponse
        {
            Status = PaymentStatus.Declined,
            Amount = payment.Amount.Value,
            Currency = Enum.Parse<Currency>(payment.Currency),
            ExpiryMonth = payment.ExpiryMonth.Value,
            ExpiryYear = payment.ExpiryYear.Value,
            Id = paymentResponse.Id,
            CardNumberLastFourDigits = GetCardLastFourDigits(payment.CardNumber),
        });

        storedPayment.Should().BeEquivalentTo(new ProcessedPayment
        {
            PaymentStatus = PaymentStatus.Declined,
            Amount = payment.Amount.Value,
            Currency = Enum.Parse<Currency>(payment.Currency),
            ExpiryMonth = payment.ExpiryMonth.Value,
            ExpiryYear = payment.ExpiryYear.Value,
            Id = paymentResponse.Id,
            CardNumberLastFourDigits = GetCardLastFourDigits(payment.CardNumber),
            AuthorisationCode = string.Empty
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
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = "GBP",
            Cvv = expectedCvv
        };
        _mockServer.Reset();
        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    card_number = paymentRequest.CardNumber,
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
            CardNumber = "12345678900876",
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
        };
        _mockServer.Reset();
        _mockServer.Given(
            Request.Create().WithPath("/payments")
                .WithBodyAsJson(new
                {
                    card_number = paymentRequest.CardNumber,
                    expiry_date = $"{paymentRequest.ExpiryMonth}/{paymentRequest.ExpiryYear}",
                    currency = paymentRequest.Currency,
                    amount = paymentRequest.Amount,
                    cvv = paymentRequest.Cvv
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
        
        paymentResponse!.CardNumberLastFourDigits.Should().BeEquivalentTo("0876");
    }
    
    private static string GetCardLastFourDigits(string cardNumber)
    {
        return cardNumber.Substring(cardNumber.Length - 4);
    }

    #region Request Validation Tests
    
    [Theory]
    [InlineData(null, "CardNumber: CardNumber is required.")]
    [InlineData("", "CardNumber: CardNumber is required.")]
    [InlineData(" ", "CardNumber: CardNumber is required.")]
    [InlineData("\n", "CardNumber: CardNumber is required.")]
    [InlineData("0", "CardNumber: Value must be between 14-19 characters long")]
    [InlineData("12345", "CardNumber: Value must be between 14-19 characters long")]
    [InlineData("1234567890987", "CardNumber: Value must be between 14-19 characters long")]
    [InlineData("12345678909873456789", "CardNumber: Value must be between 14-19 characters long")]
    [InlineData("12345er89098734567", "CardNumber: Must only contain numeric characters.")]
    public async Task GivenCardNumberIsNotValidFormat_ThenResponseShouldBeBadRequestWithCardNumberValidationError(string? cardNumber, string expectedErrorMessage)
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = cardNumber,
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
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
    
    [Theory]
    [InlineData(null, "ExpiryMonth: ExpiryMonth is required.")]
    [InlineData(0, "ExpiryMonth: Value must be between 1-12.")]
    [InlineData(12345, "ExpiryMonth: Value must be between 1-12.")]
    public async Task GivenExpiryMonthIsNotValidFormat_ThenResponseShouldBeBadRequestWithExpiryMonthValidationError(int? expiryMonth, string expectedErrorMessage)
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = expiryMonth,
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
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
    
    [Theory]
    [InlineData(null, "ExpiryYear: ExpiryYear is required.")]
    [InlineData(0, "ExpiryYear: Value must be in the future.")]
    [InlineData(12345, "ExpiryYear: Expiry month and year combination must be in the future")]
    [InlineData(2010, "ExpiryYear: Value must be in the future.")]
    public async Task GivenExpiryYearIsNotValidFormat_ThenResponseShouldBeBadRequestWithExpiryYearValidationError(int? expiryYear, string expectedErrorMessage)
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = expiryYear,
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
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
    public async Task GivenExpiryYearAndMonthCombinationNotInTheFuture_ThenResponseShouldBeBadRequestWithExpiryYearAndMonthCombinationValidationError()
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = DateTime.Now.Year,
            ExpiryMonth = DateTime.Now.Month -1,
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
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
            ErrorMessage = "ExpiryYear: Expiry month and year combination must be in the future",
        });
    }
    
    [Theory]
    [InlineData("", "Currency: Currency is required.")]
    [InlineData(" ", "Currency: Currency is required.")]
    [InlineData(null, "Currency: Currency is required.")]
    [InlineData("\n", "Currency: Currency is required.")]
    [InlineData("JYP", "Currency: Currency must be a valid ISO currency code from the following list GBP,USD,EUR.")]
    [InlineData("GB", "Currency: Currency must be 3 characters long.")]
    [InlineData("GBPS", "Currency: Currency must be 3 characters long.")]
    public async Task GivenCurrencyIsNotValid_ThenResponseShouldBeBadRequestWithCurrencyValidationError(string currency, string expectedErrorMessage)
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = currency,
            Cvv = _random.Next(100, 9999).ToString()
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
    
    [Theory]
    [InlineData(null, "Amount: Amount is required.")]
    [InlineData(-1, "Amount: Value must be greater than zero.")]
    [InlineData(0, "Amount: Value must be greater than zero.")]
    public async Task GivenAmountIsNotValid_ThenResponseShouldBeBadRequestWithAmountValidationError(int? amount, string expectedErrorMessage)
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = amount,
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
            Currency = "GBP",
            Cvv = _random.Next(100, 9999).ToString()
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
    
    [Theory]
    [InlineData("01", "Cvv: Value must be 3-4 characters long.")]
    [InlineData("32301", "Cvv: Value must be 3-4 characters long.")]
    [InlineData("", "Cvv: CVV is required.")]
    [InlineData(" ", "Cvv: CVV is required.")]
    [InlineData(null, "Cvv: CVV is required.")]
    [InlineData("\n", "Cvv: CVV is required.")]
    [InlineData("0f6", "Cvv: Value Must only contain numeric characters.")]
    public async Task GivenCvvIsNotValidFormat_ThenResponseShouldBeBadRequestWithCvvValidationError(string cvv, string expectedErrorMessage)
    {
        // Arrange
        var payment = new PostPaymentRequest
        {
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.MaxValue.Year),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumber = _random.NextInt64(10000000000000, 999999999999999999).ToString(),
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

    #endregion
}