using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SupportDesk.Infrastructure.RabbitMq;

namespace SupportDesk.UnitTests.RabbitMq;

public class RabbitMqHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenRabbitMqIsUnavailable_ReturnsUnhealthy()
    {
        var options = Options.Create(new RabbitMqOptions
        {
            HostName = "localhost",
            Port = 1,
            UserName = "localhost",
            Password = "localhost",
        });

        var healthCheck = new RabbitMqHealthCheck(options);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            cancellationToken: CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}