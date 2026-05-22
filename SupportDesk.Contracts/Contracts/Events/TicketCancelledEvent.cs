namespace SupportDesk.Contracts.Contracts.Events;

public class TicketCancelledEvent
{
    public int TicketId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset CancelledAt { get; set; }
    public required string Reason { get; set; }
}