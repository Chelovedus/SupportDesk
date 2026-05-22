namespace SupportDesk.Contracts.Contracts.Events;

public class TicketStartedProgressEvent
{
    public int TicketId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
}