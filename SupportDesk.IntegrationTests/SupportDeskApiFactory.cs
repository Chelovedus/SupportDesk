using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace SupportDesk.IntegrationTests;

public sealed class SupportDeskApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresql = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("supportdesk_tests")
        .WithUsername("supportdesk")
        .WithPassword("supportdesk")
        .Build();

    private readonly Dictionary<string, string?> _previousEnvironmentVariables = new();

    public async Task InitializeAsync()
    {
        await _postgresql.StartAsync();

        SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _postgresql.GetConnectionString());

        SetEnvironmentVariable("Database__MigrateOnStartup", "true");
        SetEnvironmentVariable("Database__SeedOnStartup", "true");

        SetEnvironmentVariable("Jwt__Issuer", "SupportDesk.Api");
        SetEnvironmentVariable("Jwt__Audience", "SupportDesk.Api");
        SetEnvironmentVariable(
            "Jwt__SecretKey",
            "test-secret-key-test-secret-key-test-secret-key-123456");

        SetEnvironmentVariable("Jwt__ExpiresMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public new async Task DisposeAsync()
    {
        RestoreEnvironmentVariables();

        await _postgresql.DisposeAsync();
        await base.DisposeAsync();
    }

    private void SetEnvironmentVariable(string name, string value)
    {
        _previousEnvironmentVariables[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    private void RestoreEnvironmentVariables()
    {
        foreach (var (name, previousValue) in _previousEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(name, previousValue);
        }
    }
}