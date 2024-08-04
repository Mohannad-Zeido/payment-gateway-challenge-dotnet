namespace PaymentGateway.Application.Services;

public interface IPaymentService
{
    public Task ProcessPaymentAsync();

}