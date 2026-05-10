namespace SupportDesk.Contracts.Requests;

public sealed class CancelTicketRequest
{
    public required string Reason { get; set; }
}