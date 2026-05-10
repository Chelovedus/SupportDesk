using SupportDesk.Domain;

namespace SupportDesk.Contracts.Responses;

public sealed class TicketResponse
{
    public TicketResponse(int id, string title, string description, TicketStatus status, TicketPriority priority, Guid createdByUserId, Guid? assignedAgentId, DateTimeOffset createdAt, DateTimeOffset updatedAt, DateTimeOffset? resolvedAt, DateTimeOffset? closedAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = status;
        Priority = priority;
        CreatedByUserId = createdByUserId;
        AssignedAgentId = assignedAgentId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        ResolvedAt = resolvedAt;
        ClosedAt = closedAt;
    }

    public int Id { get; set; }
    public string Title { get; set; }
    public string Description  { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}