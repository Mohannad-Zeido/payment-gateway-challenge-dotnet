using WireMock.Server;

namespace PaymentGateway.Api.Tests;

public class WireMockServerSetup
{
    public readonly WireMockServer MockServer = WireMockServer.Start(8080);
    
}