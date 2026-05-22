using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Contracts.Contracts.Events;
using SupportDesk.Contracts.Contracts.Responses;
using SupportDesk.Contracts.Responses;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;


namespace SupportDesk.IntegrationTests;

public class OutboxIntegrationTests : IClassFixture<SupportDeskApiFactory>
{
    private readonly SupportDeskApiFactory _factory;
    private readonly HttpClient _client;

    public OutboxIntegrationTests(SupportDeskApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateTicket_CreatesTicketCreatedOutboxMessage()
    {
        var user = await CreateUserClientAsync();

        const string ticketTitle = "Wrong password!";
        const string ticketDescription = "I miss my password. Help!";
        const string ticketPriority = "High";

        var response = await user.Client.PostAsJsonAsync("/api/tickets", new
        {
            title = ticketTitle,
            description = ticketDescription,
            priority = ticketPriority
        });

        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, responseBody);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Title.Should().Be(ticketTitle);
        body.Description.Should().Be(ticketDescription);
        body.Priority.ToString().Should().Be(ticketPriority);
        body.Status.ToString().Should().Be("New");
        
        var outboxMessage = await GetLatestOutboxMessageAsync(nameof(TicketCreatedEvent));

        outboxMessage.Type.Should().Be(nameof(TicketCreatedEvent));
        outboxMessage.Status.Should().Be(OutboxMessageStatus.Pending);
        outboxMessage.RetryCount.Should().Be(0);
        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.LastError.Should().BeNull();

        var ticketEvent = JsonSerializer.Deserialize<TicketCreatedEvent>(outboxMessage.PayloadJson);

        ticketEvent.Should().NotBeNull();
        ticketEvent!.TicketId.Should().Be(body.Id);
        ticketEvent.CreatedByUserId.Should().Be(user.UserId);
        ticketEvent.Title.Should().Be(ticketTitle);
        ticketEvent.CreatedAt.Should().Be(body.CreatedAt);
    }
    
    [Fact]
    public async Task AssignTicket_CreatesTicketAssignedOutboxMessage()
    {
        var user = await CreateUserClientAsync();
        var agent = await CreateAgentClientAsync();

        var createResponse = await user.Client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Wrong password!",
            description = "I miss my password. Help!",
            priority = "High"
        });

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createResponseBody);

        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        createdTicket.Should().NotBeNull();

        var assignResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket!.Id}/assign",
            new
            {
                agentId = agent.UserId
            });

        var assignResponseBody = await assignResponse.Content.ReadAsStringAsync();

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, assignResponseBody);

        var body = await assignResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.AssignedAgentId.Should().Be(agent.UserId);
        
        var outboxMessage = await GetLatestOutboxMessageAsync(nameof(TicketAssignedEvent));

        outboxMessage.Type.Should().Be(nameof(TicketAssignedEvent));
        outboxMessage.Status.Should().Be(OutboxMessageStatus.Pending);
        outboxMessage.RetryCount.Should().Be(0);
        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.LastError.Should().BeNull();

        var ticketEvent = JsonSerializer.Deserialize<TicketAssignedEvent>(outboxMessage.PayloadJson);

        ticketEvent.Should().NotBeNull();
        ticketEvent!.TicketId.Should().Be(body.Id);
        ticketEvent.AssignedAgentId.Should().Be(agent.UserId);
        ticketEvent.ActorUserId.Should().Be(agent.UserId);
        ticketEvent.AssignedAt.Should().Be(body.UpdatedAt);
    }
    
    [Fact]
    public async Task ResolveTicket_CreatesTicketResolvedOutboxMessage()
    {
        var user = await CreateUserClientAsync();
        var agent = await CreateAgentClientAsync();

        var createResponse = await user.Client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Wrong password!",
            description = "I miss my password. Help!",
            priority = "High"
        });

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createResponseBody);

        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        createdTicket.Should().NotBeNull();
        createdTicket!.Status.ToString().Should().Be("New");

        var assignResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/assign",
            new
            {
                agentId = agent.UserId
            });

        var assignResponseBody = await assignResponse.Content.ReadAsStringAsync();
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, assignResponseBody);

        var assignedTicket = await assignResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        assignedTicket.Should().NotBeNull();
        assignedTicket!.Status.ToString().Should().Be("Assigned");
        assignedTicket.AssignedAgentId.Should().Be(agent.UserId);

        var startResponse = await agent.Client.PostAsync(
            $"/api/tickets/{createdTicket.Id}/start",
            content: null);

        var startResponseBody = await startResponse.Content.ReadAsStringAsync();
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK, startResponseBody);

        var startedTicket = await startResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        startedTicket.Should().NotBeNull();
        startedTicket!.Status.ToString().Should().Be("InProgress");

        var createCommentResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/comments",
            new
            {
                commentText = "Password restored. Please, try log in."
            });
        
        createCommentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resolveResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/resolve",
            new
            {
                resolution = "Password restored."
            });

        var resolveResponseBody = await resolveResponse.Content.ReadAsStringAsync();
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK, resolveResponseBody);

        var resolvedTicket = await resolveResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        resolvedTicket.Should().NotBeNull();
        resolvedTicket!.Status.ToString().Should().Be("Resolved");
        
        var outboxMessage = await GetLatestOutboxMessageAsync(nameof(TicketResolvedEvent));

        outboxMessage.Type.Should().Be(nameof(TicketResolvedEvent));
        outboxMessage.Status.Should().Be(OutboxMessageStatus.Pending);
        outboxMessage.RetryCount.Should().Be(0);
        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.LastError.Should().BeNull();

        var ticketEvent = JsonSerializer.Deserialize<TicketResolvedEvent>(outboxMessage.PayloadJson);

        ticketEvent.Should().NotBeNull();
        ticketEvent!.TicketId.Should().Be(createdTicket.Id);
        ticketEvent.ActorUserId.Should().Be(agent.UserId);
        ticketEvent.ResolvedAt.Should().Be(resolvedTicket.ResolvedAt);
    }
    
    [Fact]
    public async Task CloseTicket_CreatesTicketClosedOutboxMessage()
    {
        var user = await CreateUserClientAsync();
        var agent = await CreateAgentClientAsync();

        var createResponse = await user.Client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Wrong password!",
            description = "I miss my password. Help!",
            priority = "High"
        });

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createResponseBody);

        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        createdTicket.Should().NotBeNull();
        createdTicket!.Status.ToString().Should().Be("New");

        var assignResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/assign",
            new
            {
                agentId = agent.UserId
            });

        var assignResponseBody = await assignResponse.Content.ReadAsStringAsync();
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, assignResponseBody);

        var assignedTicket = await assignResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        assignedTicket.Should().NotBeNull();
        assignedTicket!.Status.ToString().Should().Be("Assigned");
        assignedTicket.AssignedAgentId.Should().Be(agent.UserId);

        var startResponse = await agent.Client.PostAsync(
            $"/api/tickets/{createdTicket.Id}/start",
            content: null);

        var startResponseBody = await startResponse.Content.ReadAsStringAsync();
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK, startResponseBody);

        var startedTicket = await startResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        startedTicket.Should().NotBeNull();
        startedTicket!.Status.ToString().Should().Be("InProgress");

        var resolveResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/resolve",
            new
            {
                resolution = "Password restored."
            });

        var resolveResponseBody = await resolveResponse.Content.ReadAsStringAsync();
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK, resolveResponseBody);

        var resolvedTicket = await resolveResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        resolvedTicket.Should().NotBeNull();
        resolvedTicket!.Status.ToString().Should().Be("Resolved");
        resolvedTicket.ResolvedAt.Should().NotBeNull();

        var closeResponse = await user.Client.PostAsync(
            $"/api/tickets/{createdTicket.Id}/close",
            content: null);

        var closeResponseBody = await closeResponse.Content.ReadAsStringAsync();
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK, closeResponseBody);

        var closedTicket = await closeResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        closedTicket.Should().NotBeNull();
        closedTicket!.Status.ToString().Should().Be("Closed");
        closedTicket.ClosedAt.Should().NotBeNull();

        var outboxMessage = await GetLatestOutboxMessageAsync(nameof(TicketClosedEvent));

        outboxMessage.Type.Should().Be(nameof(TicketClosedEvent));
        outboxMessage.Status.Should().Be(OutboxMessageStatus.Pending);
        outboxMessage.RetryCount.Should().Be(0);
        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.LastError.Should().BeNull();

        var ticketEvent = JsonSerializer.Deserialize<TicketClosedEvent>(outboxMessage.PayloadJson);

        ticketEvent.Should().NotBeNull();
        ticketEvent!.TicketId.Should().Be(createdTicket.Id);
        ticketEvent.ActorUserId.Should().Be(user.UserId);
        ticketEvent.ClosedAt.Should().Be(closedTicket.ClosedAt);
    }
    
    [Fact]
    public async Task StartProgressTicket_CreatesTicketProgressStartedOutboxMessage()
    {
        var user = await CreateUserClientAsync();
        var agent = await CreateAgentClientAsync();

        var createResponse = await user.Client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Wrong password!",
            description = "I miss my password. Help!",
            priority = "High"
        });

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createResponseBody);

        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        createdTicket.Should().NotBeNull();
        createdTicket!.Status.ToString().Should().Be("New");

        var assignResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/assign",
            new
            {
                agentId = agent.UserId
            });

        var assignResponseBody = await assignResponse.Content.ReadAsStringAsync();
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, assignResponseBody);

        var assignedTicket = await assignResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        assignedTicket.Should().NotBeNull();
        assignedTicket!.Status.ToString().Should().Be("Assigned");
        assignedTicket.AssignedAgentId.Should().Be(agent.UserId);

        var startResponse = await agent.Client.PostAsync(
            $"/api/tickets/{createdTicket.Id}/start",
            content: null);

        var startResponseBody = await startResponse.Content.ReadAsStringAsync();
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK, startResponseBody);

        var startedTicket = await startResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        startedTicket.Should().NotBeNull();
        startedTicket!.Status.ToString().Should().Be("InProgress");

        var outboxMessage = await GetLatestOutboxMessageAsync(nameof(TicketStartedProgressEvent));

        outboxMessage.Type.Should().Be(nameof(TicketStartedProgressEvent));
        outboxMessage.Status.Should().Be(OutboxMessageStatus.Pending);
        outboxMessage.RetryCount.Should().Be(0);
        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.LastError.Should().BeNull();

        var ticketEvent = JsonSerializer.Deserialize<TicketStartedProgressEvent>(outboxMessage.PayloadJson);

        ticketEvent.Should().NotBeNull();
        ticketEvent!.TicketId.Should().Be(createdTicket.Id);
        ticketEvent.ActorUserId.Should().Be(agent.UserId);
        ticketEvent.StartedAt.Should().Be(startedTicket.UpdatedAt);
    }
    
    [Fact]
    public async Task CancelTicket_CreatesTicketCancelledOutboxMessage()
    {
        var user = await CreateUserClientAsync();

        var createResponse = await user.Client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Wrong password!",
            description = "I miss my password. Help!",
            priority = "High"
        });

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createResponseBody);

        var createdTicket = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        createdTicket.Should().NotBeNull();
        createdTicket!.Status.ToString().Should().Be("New");

        const string reason = "Created by mistake.";

        var cancelResponse = await user.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/cancel",
            new
            {
                reason
            });

        var cancelResponseBody = await cancelResponse.Content.ReadAsStringAsync();
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK, cancelResponseBody);

        var cancelledTicket = await cancelResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        cancelledTicket.Should().NotBeNull();
        cancelledTicket!.Status.ToString().Should().Be("Cancelled");

        var outboxMessage = await GetLatestOutboxMessageAsync(nameof(TicketCancelledEvent));

        outboxMessage.Type.Should().Be(nameof(TicketCancelledEvent));
        outboxMessage.Status.Should().Be(OutboxMessageStatus.Pending);
        outboxMessage.RetryCount.Should().Be(0);
        outboxMessage.ProcessedAt.Should().BeNull();
        outboxMessage.LastError.Should().BeNull();

        var ticketEvent = JsonSerializer.Deserialize<TicketCancelledEvent>(outboxMessage.PayloadJson);

        ticketEvent.Should().NotBeNull();
        ticketEvent!.TicketId.Should().Be(createdTicket.Id);
        ticketEvent.ActorUserId.Should().Be(user.UserId);
        ticketEvent.CancelledAt.Should().Be(cancelledTicket.UpdatedAt);
        ticketEvent.Reason.Should().Be(reason);
    }
    
    private Task<AuthenticatedClient> CreateUserClientAsync()
    {
        return CreateAuthenticatedClientWithUserIdAsync(
            email: "user@example.com",
            password: "Password123!");
    }
    
    private Task<AuthenticatedClient> CreateUserSecondClientAsync()
    {
        return CreateAuthenticatedClientWithUserIdAsync(
            email: "usersecond@example.com",
            password: "Password123!");
    }

    private Task<AuthenticatedClient> CreateAgentClientAsync()
    {
        return CreateAuthenticatedClientWithUserIdAsync(
            email: "agent@example.com",
            password: "Password123!");
    }
    
    private Task<AuthenticatedClient> CreateAdminClientAsync()
    {
        return CreateAuthenticatedClientWithUserIdAsync(
            email: "admin@example.com",
            password: "Password123!");
    }
    
    private sealed record AuthenticatedClient(HttpClient Client, Guid UserId);
    
    private async Task<AuthenticatedClient> CreateAuthenticatedClientWithUserIdAsync(
        string email,
        string password)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.AccessToken);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);

        var userIdValue = token.Claims
            .SingleOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub)
            ?.Value;
        
        userIdValue.Should().NotBeNullOrWhiteSpace();
        
        var userId = Guid.Parse(userIdValue!);

        return new AuthenticatedClient(client, userId);
    }
    
    private async Task<OutboxMessage> GetLatestOutboxMessageAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();

        var outboxMessage = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Type == eventType)
            .OrderByDescending(message => message.CreatedAt)
            .FirstAsync(cancellationToken);

        return outboxMessage;
    }
    
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
    
}