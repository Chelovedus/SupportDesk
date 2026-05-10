namespace SupportDesk.Domain;

public class TicketHistoryItem
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; }
    public string OldStatus { get; private set; }
    public string NewStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string Details { get; private set; }
    
    public TicketHistoryItem(string action, string details, int ticketId, Guid actorUserId, string oldStatus, string newStatus, DateTimeOffset createdAt)
    {
        Action = action;
        Details = details;
        TicketId = ticketId;
        ActorUserId = actorUserId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        CreatedAt = createdAt;
    }
}
