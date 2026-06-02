# SupportDesk API

The project demonstrates a backend flow with PostgreSQL persistence, JWT authorization, ticket lifecycle management, transactional outbox, RabbitMQ event publishing, notification worker, health checks and automated tests.

<img width="1672" height="941" alt="image-сжатый" src="https://github.com/user-attachments/assets/8013d4e4-5224-4a10-9b0e-f80e49399ab8" />

## Current scope

This README describes the current state of the project after the MVP and production-oriented extensions.

Implemented:

- ASP.NET Core Web API
- Controller-based REST API
- PostgreSQL persistence
- Entity Framework Core migrations
- Ticket lifecycle
- Ticket comments
- Ticket history
- JWT authentication
- Role-based authorization
- Seed demo users
- Transactional outbox
- Background outbox processor
- RabbitMQ event publishing
- NotificationWorker RabbitMQ consumer
- Correlation id middleware
- Liveness and readiness health checks
- PostgreSQL readiness check
- RabbitMQ readiness check
- Filtering, sorting, and pagination for ticket queries
- Unit tests
- Integration tests with Testcontainers
- Docker Compose for local infrastructure
- Swagger/OpenAPI documentation

Not included yet:

- Frontend application
- User registration
- Redis
- gRPC
- Prometheus metrics
- File attachments
- Real email notifications
- Kubernetes

These features are intentionally left for later iterations.

## Tech stack

- C#
- .NET
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- Hosted background services
- Transactional outbox
- Health checks
- JWT Bearer authentication
- xUnit
- FluentAssertions
- WebApplicationFactory
- Testcontainers for .NET
- Docker Compose
- Swagger/OpenAPI

## Architecture

The solution is split into separate projects to keep HTTP, application logic, domain rules, and infrastructure concerns isolated.

```text
NotificationWorker
  RabbitMQ consumer that processes ticket events and writes notification logs

RabbitMQ
  Message broker for asynchronous ticket events

SupportDesk.Api
  HTTP API, controllers, authentication, authorization, Swagger, dependency injection

SupportDesk.Application
  Application services and use cases

SupportDesk.Domain
  Domain entities, enums, lifecycle rules, domain exceptions

SupportDesk.Infrastructure
  EF Core DbContext, entity configurations, migrations, persistence, auth infrastructure

SupportDesk.Contracts
  Request and response contracts shared by API and tests

SupportDesk.UnitTests
  Unit tests for domain rules

SupportDesk.IntegrationTests
  End-to-end HTTP tests with Testcontainers and a real PostgreSQL instance
```

```mermaid
flowchart LR
    Client[Swagger / HTTP client] --> Api[SupportDesk.Api]
    Api --> Application[SupportDesk.Application]
    Application --> Domain[SupportDesk.Domain]
    Application --> Infrastructure[SupportDesk.Infrastructure]
    Infrastructure --> Db[(PostgreSQL)]

    Infrastructure --> Outbox[(Outbox messages)]
    Outbox --> Processor[OutboxProcessorBackgroundService]
    Processor --> RabbitMQ[(RabbitMQ)]
    RabbitMQ --> Worker[NotificationWorker]

    IntegrationTests[Integration tests] --> Api
    IntegrationTests --> Testcontainers[(PostgreSQL Testcontainer)]
```

## Ticket lifecycle

```mermaid
stateDiagram-v2
    [*] --> New
    New --> Assigned
    New --> Cancelled
    Assigned --> InProgress
    Assigned --> Cancelled
    InProgress --> Resolved
    InProgress --> Cancelled
    Resolved --> Closed
    Resolved --> InProgress
    Closed --> [*]
    Cancelled --> [*]
```

Business rules are enforced by the domain model and application layer. A ticket status cannot be changed by assigning the `Status` property directly from the outside.

## Messaging and outbox

Ticket status changes create outbox messages in the same database transaction as the ticket update and history record.

The background outbox processor reads pending messages and publishes them to RabbitMQ. NotificationWorker consumes ticket events from the queue and logs notification handling.

This keeps the API independent from the notification consumer. If NotificationWorker is stopped, messages remain in RabbitMQ until the consumer is available again.

## Roles

| Role | Permissions |
| --- | --- |
| User | Creates tickets, views own tickets, comments on own tickets, closes resolved tickets |
| SupportAgent | Views available or assigned tickets, assigns tickets to self, starts progress, comments, resolves tickets |
| Admin | Views all tickets and has administrative access to ticket data |

## Demo users

The application uses seeded users for the MVP. Registration is not implemented intentionally.

| Role | Email | Password |
| --- | --- | --- |
| User | `user@example.com` | `Password123!` |
| SupportAgent | `agent@example.com` | `Password123!` |
| Admin | `admin@example.com` | `Password123!` |

## API overview

### Auth

| Method | Endpoint | Description |
| --- | --- | --- |
| POST | `/api/auth/login` | Returns JWT access token |

### Tickets

| Method | Endpoint | Description |
| --- | --- | --- |
| POST | `/api/tickets` | Create a ticket |
| GET | `/api/tickets` | Search tickets with filters, sorting, and pagination |
| GET | `/api/tickets/{id}` | Get ticket by id |
| POST | `/api/tickets/{id}/assign` | Assign ticket |
| POST | `/api/tickets/{id}/start` | Move ticket to InProgress |
| POST | `/api/tickets/{id}/resolve` | Resolve ticket |
| POST | `/api/tickets/{id}/close` | Close resolved ticket |
| POST | `/api/tickets/{id}/cancel` | Cancel ticket |

### Comments

| Method | Endpoint | Description |
| --- | --- | --- |
| POST | `/api/tickets/{id}/comments` | Add comment to ticket |
| GET | `/api/tickets/{id}/comments` | Get ticket comments |

### History

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/tickets/{id}/history` | Get ticket status history |

## Query example

```http
GET /api/tickets?status=New&status=InProgress&priority=High&page=1&pageSize=20&sortBy=CreatedAt&sortDirection=Descending
Authorization: Bearer <access_token>
```

The list endpoint applies filtering, sorting, and pagination at the database query level. It does not load all tickets into memory before filtering.

## Health checks

| Endpoint | Purpose |
| --- | --- |
| `/health/live` | Checks that the API process is alive |
| `/health/ready` | Checks that the API is ready to handle traffic and dependencies are available |

Readiness checks include:

- PostgreSQL
- RabbitMQ

Check liveness:
```powershell
curl.exe -i http://localhost:5042/health/live
```

Check readiness:
```powershell
curl.exe -i http://localhost:5042/health/ready
```

## Requirements

- .NET SDK compatible with the project
- Docker Desktop or Docker Engine
- Git
- Optional: PostgreSQL client, DBeaver, Rider, Visual Studio, or VS Code

Docker must be running for both local PostgreSQL and integration tests with Testcontainers.

## Run locally:

<img width="891" height="330" alt="run" src="https://github.com/user-attachments/assets/d7d20a2d-49d1-4f23-8ee9-097af1c125ec" />

Start local infrastructure through Docker Compose:

```bash
docker compose up -d
```
This starts:
- PostgreSQL
- RabbitMQ

Apply EF Core migrations:

```powershell
dotnet ef database update --project .\SupportDesk.Infrastructure\SupportDesk.Infrastructure.csproj --startup-project .\SupportDesk.Api\SupportDesk.Api.csproj
```

Run the API:

```powershell
dotnet run --project .\SupportDesk.Api\SupportDesk.Api.csproj
```

Run the NotificationWorker:

```powershell
dotnet run --project .\NotificationWorker\NotificationWorker.csproj
```

Open Swagger:

```text
http://localhost:5042/swagger
```

If the application starts on a different port, use the URL printed by `dotnet run`.

## Run tests

```powershell
dotnet test
```

The integration tests use Testcontainers to start a real PostgreSQL container during the test run. This means the tests do not depend on a manually prepared local database.

<img width="885" height="314" alt="dotnet-test" src="https://github.com/user-attachments/assets/eacd95ae-bf18-4e5f-ad44-b1e67d68fa00" />


The test suite covers:

- Domain lifecycle rules
- Authentication
- Authorization
- Ticket creation
- Ticket visibility by role
- Full ticket lifecycle through HTTP
- Persistence through EF Core and PostgreSQL

## Example login request

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}
```

Example response:

```json
{
  "email": "user@example.com",
  "userId": "019e1182-7e18-7d04-a35d-8d48115cda62",
  "role": "User",
  "accessToken": "<jwt_access_token>",
  "expiresAt": "2026-05-14T12:00:00Z"
}
```
<img width="519" height="397" alt="Login" src="https://github.com/user-attachments/assets/674da134-bb52-4b87-b0f9-37722877310d" />

## Example ticket creation request

```http
POST /api/tickets
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "title": "Cannot access internal portal",
  "description": "The user cannot sign in to the internal company portal.",
  "priority": "High"
}
```
<img width="519" height="397" alt="Create Ticket" src="https://github.com/user-attachments/assets/d617516b-4563-4d1c-a369-aa4f02b4eaf6" />


## Suggested demo flow

The project can be demonstrated without a frontend.

Recommended flow:

1. Start PostgreSQL and RabbitMQ with Docker Compose.
2. Apply EF Core migrations.
3. Run the API.
4. Open Swagger.
5. Login as `user@example.com`.
6. Create a ticket.
7. Login as `agent@example.com`.
8. Assign the ticket.
9. Move the ticket to `InProgress`.
10. Add a comment.
11. Resolve the ticket.
12. Login as `user@example.com` again.
13. Close the resolved ticket.
14. Open ticket history.
15. Run `dotnet test` and show that integration tests pass with Testcontainers.
16. Verify that outbox messages are created.
17. Run NotificationWorker.
18. Trigger ticket status changes.
19. Check API logs for outbox processing.
20. Check NotificationWorker logs for consumed ticket events.
21. Check `/health/live`.
22. Check `/health/ready`.
23. Stop RabbitMQ and verify that `/health/ready` returns `503 Service Unavailable`.

## Current limitations

- Authentication uses seeded demo users.
- There is no registration flow.
- There is no frontend.
- There are no real notifications.
- There is no Redis cache yet.
- There is no gRPC service yet.
- There are no Prometheus-compatible production metrics yet.

These limitations are deliberate. The current version focuses on a complete backend flow, database persistence, authorization, outbox-based messaging, RabbitMQ integration, health checks, and automated testing.

## Roadmap

Planned production-oriented extensions:

- gRPC UserDirectory service
- Redis cache for ticket statistics
- Prometheus-compatible metrics
- k6 load test scenario
- SQL performance notes

## What this project demonstrates

This project is designed to show backend engineering fundamentals:

- Separating HTTP, application, domain, and infrastructure layers
- Modeling business rules in the domain instead of controllers
- Persisting data through EF Core and PostgreSQL
- Using migrations to evolve the database schema
- Protecting endpoints with JWT authentication and role-based authorization
- Testing real HTTP behavior with WebApplicationFactory
- Running integration tests against real PostgreSQL through Testcontainers
- Evolving an MVP backend into a production-oriented service step by step
