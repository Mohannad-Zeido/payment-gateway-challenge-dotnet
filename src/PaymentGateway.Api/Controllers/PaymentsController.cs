using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Application.Enums;
using PaymentGateway.Application.Models;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;

    public PaymentsController(PaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
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
        var result = await requestValidator.ValidateAsync(request);

        var processPaymentRequest = request.ToProcessPaymentRequest();

        
        
        var payment = _paymentsRepository.Get(Guid.NewGuid());

        return new OkObjectResult(payment);
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
            Cvv = postPaymentRequest.Cvv!.Value,
        };
    }
}