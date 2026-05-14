using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SupportDesk.Contracts.Contracts.Responses;
using SupportDesk.Contracts.Responses;

namespace SupportDesk.IntegrationTests;

public sealed class TicketsIntegrationTests : IClassFixture<SupportDeskApiFactory>
{
    private readonly SupportDeskApiFactory _factory;
    private readonly HttpClient _client;

    public TicketsIntegrationTests(SupportDeskApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidUser_ReturnsAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "user@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreateTicket_WithValidUser_ReturnsCreatedNewTicketResponse()
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
    }

    [Fact]
    public async Task CreateTicket_WithoutAuthorizationHeader_ReturnsUnauthorized()
    {
        const string ticketTitle = "Help!";
        const string ticketDescription = "My phone was broken.";
        const string ticketPriority = "High";

        var response = await _client.PostAsJsonAsync("/api/tickets", new
        {
            title = ticketTitle,
            description = ticketDescription,
            priority = ticketPriority
        });

        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, responseBody);
    }

    [Fact]
    public async Task CreateTicket_WithAgentCredentials_ReturnsForbidden()
    {
        var agent = await CreateAgentClientAsync();
        
        const string ticketTitle = "Help!";
        const string ticketDescription = "My phone was broken.";
        const string ticketPriority = "High";

        var response = await agent.Client.PostAsJsonAsync("/api/tickets", new
        {
            title = ticketTitle,
            description = ticketDescription,
            priority = ticketPriority
        });

        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, responseBody);
    }

    [Fact]
    public async Task AssignTo_WithUserCredentials_ReturnsForbidden()
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

        var assignResponse = await user.Client.PostAsJsonAsync($"/api/tickets/{createdTicket!.Id}/assign",
            new
            {
                agentId = Guid.NewGuid()
            });

        var assignResponseBody = await assignResponse.Content.ReadAsStringAsync();

        assignResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, assignResponseBody);
    }
    
    [Fact]
    public async Task AssignTo_WithAgentCredentials_ReturnsTicketResponse()
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
    }
    
    [Fact]
    public async Task AssignTicket_WhenAgentAssignsTicketToAnotherAgent_ReturnsBadRequest()
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
                agentId = Guid.CreateVersion7()
            });

        var assignResponseBody = await assignResponse.Content.ReadAsStringAsync();

        assignResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, assignResponseBody);
    }
    
    [Fact]
    public async Task FullTicketLifecycle_WithValidUserAndAgent_CompletesSuccessfully()
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

        var closeResponse = await user.Client.PostAsync(
            $"/api/tickets/{createdTicket.Id}/close",
            content: null);

        var closeResponseBody = await closeResponse.Content.ReadAsStringAsync();
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK, closeResponseBody);

        var closedTicket = await closeResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        closedTicket.Should().NotBeNull();
        closedTicket!.Status.ToString().Should().Be("Closed");

        var commentsResponse = await user.Client.GetAsync(
            $"/api/tickets/{createdTicket.Id}/comments");

        var commentsResponseBody = await commentsResponse.Content.ReadAsStringAsync();
        commentsResponse.StatusCode.Should().Be(HttpStatusCode.OK, commentsResponseBody);

        commentsResponseBody.Should().Contain("Password restored. Please, try log in.");

        var historyResponse = await agent.Client.GetAsync(
            $"/api/tickets/{createdTicket.Id}/history");

        var historyResponseBody = await historyResponse.Content.ReadAsStringAsync();
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK, historyResponseBody);

        historyResponseBody.Should().Contain("Assigned");
        historyResponseBody.Should().Contain("InProgress");
        historyResponseBody.Should().Contain("Resolved");
        historyResponseBody.Should().Contain("Closed");
    }
    
    [Fact]
    public async Task GetTicket_ForAnotherUsersTicket_ReturnsNotFound()
    {
        var user = await CreateUserClientAsync();
        var userSecond = await CreateUserSecondClientAsync();

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

        var ticketResponse = await userSecond.Client.GetAsync($"/api/tickets/{createdTicket.Id}");

        ticketResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetTicketHistoryAndComments_ForAnotherUsersTicket_ReturnsNotFound()
    {
        var user = await CreateUserClientAsync();
        var secondUser = await CreateUserSecondClientAsync();
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

        var closeResponse = await user.Client.PostAsync(
            $"/api/tickets/{createdTicket.Id}/close",
            content: null);

        var closeResponseBody = await closeResponse.Content.ReadAsStringAsync();
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK, closeResponseBody);

        var closedTicket = await closeResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        closedTicket.Should().NotBeNull();
        closedTicket!.Status.ToString().Should().Be("Closed");

        var commentsResponse = await user.Client.GetAsync(
            $"/api/tickets/{createdTicket.Id}/comments");

        var commentsResponseBody = await commentsResponse.Content.ReadAsStringAsync();
        commentsResponse.StatusCode.Should().Be(HttpStatusCode.OK, commentsResponseBody);

        commentsResponseBody.Should().Contain("Password restored. Please, try log in.");

        var historyResponse = await agent.Client.GetAsync(
            $"/api/tickets/{createdTicket.Id}/history");

        var historyResponseBody = await historyResponse.Content.ReadAsStringAsync();
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK, historyResponseBody);

        historyResponseBody.Should().Contain("Assigned");
        historyResponseBody.Should().Contain("InProgress");
        historyResponseBody.Should().Contain("Resolved");
        historyResponseBody.Should().Contain("Closed");

        var secondHistoryResponse = await secondUser.Client.GetAsync($"/api/tickets/{createdTicket.Id}/history");

        secondHistoryResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var secondCommentsResponse = await secondUser.Client.GetAsync($"/api/tickets/{createdTicket.Id}/comments");
        
        secondCommentsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task CloseAndCancelTicket_WithAgentCredentials_ReturnsForbidden()
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

        var closeResponse = await agent.Client.PostAsync(
            $"/api/tickets/{createdTicket!.Id}/close",
            content: null);

        var closeResponseBody = await closeResponse.Content.ReadAsStringAsync();
        closeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, closeResponseBody);

        var cancelResponse = await agent.Client.PostAsJsonAsync(
            $"/api/tickets/{createdTicket.Id}/cancel",
            new
            {
                reason = "Trying to cancel as agent."
            });

        var cancelResponseBody = await cancelResponse.Content.ReadAsStringAsync();
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, cancelResponseBody);
    }
    
    [Fact]
    public async Task GetTickets_WithInvalidPagination_ReturnsBadRequest()
    {
        var user = await CreateUserClientAsync();

        var invalidPageResponse = await user.Client.GetAsync(
            "/api/tickets?page=0&pageSize=20");

        invalidPageResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var invalidPageSizeResponse = await user.Client.GetAsync(
            "/api/tickets?page=1&pageSize=101");

        invalidPageSizeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task GetTickets_WithAdminCredentials_ReturnsCreatedTicket()
    {
        var user = await CreateUserClientAsync();
        var admin = await CreateAdminClientAsync();

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

        var getTicketsResponse = await admin.Client.GetAsync("/api/tickets");

        var getTicketsResponseBody = await getTicketsResponse.Content.ReadAsStringAsync();
        getTicketsResponse.StatusCode.Should().Be(HttpStatusCode.OK, getTicketsResponseBody);

        var tickets = await getTicketsResponse.Content
            .ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>(JsonOptions);

        tickets.Should().NotBeNull();
        tickets!.Items.Should().Contain(ticket => ticket.Id == createdTicket!.Id);
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
    
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}