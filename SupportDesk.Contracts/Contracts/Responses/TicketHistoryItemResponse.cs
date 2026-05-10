namespace SupportDesk.Contracts.Responses;

public sealed class TicketHistoryItemResponse
{
    public TicketHistoryItemResponse(int id, int ticketId, Guid actorUserId, string action, string oldStatus, string newStatus, DateTimeOffset createdAt, string details)
    {
        Id = id;
        TicketId = ticketId;
        ActorUserId = actorUserId;
        Action = action;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        CreatedAt = createdAt;
        Details = details;
    }

    public int Id { get; set; }
    public int TicketId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; }
    public string OldStatus { get; set; }
    public string NewStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Details { get; set; }
}