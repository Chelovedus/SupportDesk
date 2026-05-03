namespace SupportDesk.Contracts.Requests;

public sealed class ResolveTicketRequest
{
    public int ActorId { get; set; }
    public required string Resolution { get; set; }
}