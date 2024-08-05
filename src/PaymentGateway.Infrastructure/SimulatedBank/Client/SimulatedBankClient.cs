using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PaymentGateway.Infrastructure.Configuration;
using PaymentGateway.Infrastructure.SimulatedBank.Models;

namespace PaymentGateway.Infrastructure.SimulatedBank.Client;

public class SimulatedBankClient : ISimulatedBankClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SimulatedBankOptions> _simulatedBankOptions;
    private readonly ILogger<SimulatedBankClient> _logger;

    public SimulatedBankClient(IOptions<SimulatedBankOptions> simulatedBankOptions, ILogger<SimulatedBankClient> logger)
    {
        _simulatedBankOptions = simulatedBankOptions;
        _logger = logger;
        _httpClient = new HttpClient();
    }
    
    public async Task<PaymentsResponse> PostPaymentAsync(PaymentsRequest paymentsRequest)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, 
            RequestUri =new Uri(_simulatedBankOptions.Value.BaseUri + "/payments", UriKind.RelativeOrAbsolute ),
            Content = new StringContent(JsonSerializer.Serialize(paymentsRequest), Encoding.UTF8, "application/json")
        };
        
        _logger.LogInformation("Post request to Uri: {uri}", request.RequestUri);
        var response = await _httpClient.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        
        string responseBody = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<PaymentsResponse>(responseBody) ?? new PaymentsResponse
        {
             Authorized = false
        };
    }
}