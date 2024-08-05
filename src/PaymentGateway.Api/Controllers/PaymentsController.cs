using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Domain.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure;
using PaymentGateway.Infrastructure.Persistence;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentsRepository paymentsRepository, IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        _logger.LogInformation("Retrieving payment for Id: {payment_id}", id);
        
        var payment = await _paymentsRepository.GetAsync(id);

        if (payment is null)
        {
            _logger.LogInformation("No payment found for Id: {payment_id}", id);
            return NotFound();
        }
        
        return new OkObjectResult(new PostPaymentResponse
        {
            Amount = payment.Amount,
            Currency = payment.Currency,
            Id = id,
            Status = payment.PaymentStatus,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            CardNumberLastFourDigits = payment.CardNumberLastFourDigits
        });
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
        [FromBody] PostPaymentRequest request)
    {
        _logger.LogInformation("Process payment request received");
        
        var requestValidator = new ProcessPaymentRequestValidator();
        
        var validationResult = await requestValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var error = validationResult.Errors.First();
            var validationErrorMessageString = $"{error.PropertyName}: {error.ErrorMessage}";
            _logger.LogInformation("Validation Error: '{validation_error_message}'", validationErrorMessageString);
            return BadRequest(new RejectedPostPaymentResponse
            {
                ErrorMessage = $"{error.PropertyName}: {error.ErrorMessage}", 
                Status = PaymentStatus.Rejected,
            });
        }
        
        var processPaymentRequest = request.ToProcessPaymentRequest();
        
        var response =  await _paymentService.ProcessPaymentAsync(processPaymentRequest);
        
        var id = await _paymentsRepository.AddAsync(processPaymentRequest, response);
        
        _logger.LogInformation("Status for PaymentId: {payment_id} is: {payment_status}", id, response.PaymentStatus);
        
        return new OkObjectResult(new PostPaymentResponse
        {
            Amount = processPaymentRequest.Amount,
            Currency = processPaymentRequest.Currency,
            Id = id,
            Status = response.PaymentStatus,
            ExpiryMonth = processPaymentRequest.ExpiryMonth,
            ExpiryYear = processPaymentRequest.ExpiryYear,
            CardNumberLastFourDigits = CardNumberMasker.GetLastFourDigits(processPaymentRequest.CardNumber)
        });
    }
}

public static class PostPaymentRequestExtensions{

    public static ProcessPaymentRequest ToProcessPaymentRequest(this PostPaymentRequest postPaymentRequest)
    {
        return new ProcessPaymentRequest
        {
            ExpiryYear = postPaymentRequest.ExpiryYear!.Value,
            ExpiryMonth = postPaymentRequest.ExpiryMonth!.Value,
            Currency = Enum.Parse<Currency>(postPaymentRequest.Currency!),
            CardNumber = long.Parse(postPaymentRequest.CardNumber!),
            Amount = postPaymentRequest.Amount!.Value,
            Cvv = postPaymentRequest.Cvv!,
        };
    }
}