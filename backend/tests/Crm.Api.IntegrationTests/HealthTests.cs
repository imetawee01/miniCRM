using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Crm.Api.IntegrationTests;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public HealthTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Live_health_endpoint_responds()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        // May be 200 even if SQL is down because live is not tagged ready
        Assert.True((int)response.StatusCode is 200 or 503);
    }
}
