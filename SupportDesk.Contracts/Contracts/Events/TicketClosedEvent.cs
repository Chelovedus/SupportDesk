namespace SupportDesk.Contracts.Contracts.Events;

public class TicketClosedEvent
{
    public int TicketId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset ClosedAt { get; set; }
}