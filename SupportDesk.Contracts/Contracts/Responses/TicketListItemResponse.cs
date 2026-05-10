using SupportDesk.Domain;

namespace SupportDesk.Contracts.Responses;

public class TicketListItemResponse
{
    public TicketListItemResponse(int id, string? title, TicketStatus status, TicketPriority priority, DateTimeOffset createdAt, Guid? assignedAgentId)
    {
        Id = id;
        Title = title;
        Status = status;
        Priority = priority;
        CreatedAt = createdAt;
        AssignedAgentId = assignedAgentId;
    }

    public int Id  { get; set; }
    public string? Title { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? AssignedAgentId { get; set; }
}