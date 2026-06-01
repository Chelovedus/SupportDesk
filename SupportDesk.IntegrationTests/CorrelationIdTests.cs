using FluentAssertions;
using SupportDesk.Middleware;

namespace SupportDesk.IntegrationTests;

public sealed class CorrelationIdTests : IClassFixture<SupportDeskApiFactory>
{
    private const string HealthUri = "/api/health";
    private readonly HttpClient _client;

    public CorrelationIdTests(SupportDeskApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Request_WithoutCorrelationId_ReturnsNewCorrelationId()
    {
        var response = await _client.GetAsync(requestUri: HealthUri);

        response.Headers.TryGetValues(
                name: CorrelationIdMiddleware.HeaderName,
                out var values)
            .Should().BeTrue();

        var correlationId = values.Should().ContainSingle().Subject;

        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Request_WithCorrelationId_ReturnsSameCorrelationId()
    {
        const string correlationId = "test-correlation-id";
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthUri);
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        var response = await _client.SendAsync(request: request);

        response.EnsureSuccessStatusCode();
        
        response.Headers.TryGetValues(
                name: CorrelationIdMiddleware.HeaderName,
                out var values)
            .Should().BeTrue();

        values.Should().ContainSingle().Which.Should().Be(correlationId);
    }
}