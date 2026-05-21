namespace SupportDesk.Contracts.Contracts.Events;

public class TicketCreatedEvent
{
    public int TicketId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}