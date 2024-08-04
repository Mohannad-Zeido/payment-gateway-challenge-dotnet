using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using PaymentGateway.Infrastructure.Configuration;
using PaymentGateway.Infrastructure.SimulatedBank.Models;

namespace PaymentGateway.Infrastructure.SimulatedBank.Client;

public class SimulatedBankClient : ISimulatedBankClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SimulatedBankOptions> _simulatedBankOptions;

    public SimulatedBankClient(IOptions<SimulatedBankOptions> simulatedBankOptions)
    {
        _simulatedBankOptions = simulatedBankOptions;
        _httpClient = new HttpClient();
    }
    
    public async Task<PaymentsResponse> PostPaymentAsync(PaymentsRequest paymentsRequest)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, 
            RequestUri =new Uri(_simulatedBankOptions.Value.BaseUri + "/payments", UriKind.RelativeOrAbsolute )
        };
        
        request.Content = new StringContent(JsonSerializer.Serialize(paymentsRequest), Encoding.UTF8, "application/json");


        var response = await _httpClient.SendAsync(request);
        
        response.EnsureSuccessStatusCode();

        // Read and display the response body
        string responseBody = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<PaymentsResponse>(responseBody) ?? new PaymentsResponse
        {
             Authorized = false
        };
    }
}