using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Infrastructure.SimulatedBank;
using PaymentGateway.Infrastructure.SimulatedBank.Client;

namespace PaymentGateway.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddSimulatedBank(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SimulatedBankOptions>(configuration.GetSection(nameof(SimulatedBankOptions)));
        services.AddScoped<IPaymentService, SimulatedBankPaymentService>();
        services.AddScoped<ISimulatedBankClient, SimulatedBankClient>();
        
        return services;
    }
}