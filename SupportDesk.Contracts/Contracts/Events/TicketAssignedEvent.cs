namespace SupportDesk.Contracts.Contracts.Events;

public class TicketAssignedEvent
{
    public int TicketId { get; set; }
    public Guid AssignedAgentId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}