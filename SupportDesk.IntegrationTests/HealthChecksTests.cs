using System.Net;
using FluentAssertions;

namespace SupportDesk.IntegrationTests;

public class HealthChecksTests : IClassFixture<SupportDeskApiFactory>
{
    private const string HealthUri = "/health/live";
    private readonly HttpClient _client;

    public HealthChecksTests(SupportDeskApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveHealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync(HealthUri);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Be("Healthy");
    }
}