namespace SupportDesk.Contracts.Contracts.Events;

public class TicketResolvedEvent
{
    public int TicketId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset ResolvedAt { get; set; }
}