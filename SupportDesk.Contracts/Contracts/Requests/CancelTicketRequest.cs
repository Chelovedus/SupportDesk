namespace SupportDesk.Contracts.Requests;

public sealed class CancelTicketRequest
{
    public int ActorId { get; set; }
    public required string Reason { get; set; }
}