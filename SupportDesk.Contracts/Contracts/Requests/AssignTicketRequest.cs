namespace SupportDesk.Contracts.Requests;

public sealed class AssignTicketRequest
{
    public int AgentId { get; set; }
    public int ActorId { get; set; }
}