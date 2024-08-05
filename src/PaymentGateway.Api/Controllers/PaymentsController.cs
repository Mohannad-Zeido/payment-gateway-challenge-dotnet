using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Domain.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Domain;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IPaymentService _paymentService;

    public PaymentsController(PaymentsRepository paymentsRepository, IPaymentService paymentService)
    {
        _paymentsRepository = paymentsRepository;
        _paymentService = paymentService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment is null)
        {
            return NotFound();
        }
        await Task.Delay(1000);
        return new OkObjectResult(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync(
        [FromBody] PostPaymentRequest request)
    {
        var requestValidator = new ProcessPaymentRequestValidator();
        var validationResult = await requestValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var error = validationResult.Errors.Single();
            return BadRequest(new RejectedPostPaymentResponse
            {
                ErrorMessage = $"{error.PropertyName}: {error.ErrorMessage}", 
                Status = PaymentStatus.Rejected,
            });

        }
        
        var processPaymentRequest = request.ToProcessPaymentRequest();

       var response =  await _paymentService.ProcessPaymentAsync(processPaymentRequest);

        
        var id = _paymentsRepository.Add(processPaymentRequest, response);

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
            CardNumber = postPaymentRequest.CardNumber!.Value,
            Amount = postPaymentRequest.Amount!.Value,
            Cvv = postPaymentRequest.Cvv!,
        };
    }
}