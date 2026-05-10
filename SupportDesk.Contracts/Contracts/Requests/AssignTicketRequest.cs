namespace SupportDesk.Contracts.Requests;

public sealed class AssignTicketRequest
{
    public Guid AgentId { get; set; }
}