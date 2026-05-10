namespace SupportDesk.Contracts.Requests;

public sealed class ResolveTicketRequest
{
    public required string Resolution { get; set; }
}