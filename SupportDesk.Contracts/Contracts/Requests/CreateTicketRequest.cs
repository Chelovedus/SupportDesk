using SupportDesk.Domain;

namespace SupportDesk.Contracts.Requests;

public sealed class CreateTicketRequest
{
    public required string Title { get; set; }
    public required string Description  { get; set; }
    public TicketPriority Priority { get; set; }
}