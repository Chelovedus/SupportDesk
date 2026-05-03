using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;

namespace SupportDesk.Application.Tickets;

public interface ITicketService
{
    TicketResponse CreateTicket(CreateTicketRequest request);
    TicketResponse? GetTicketById(int ticketId);
    IReadOnlyCollection<TicketListItemResponse> GetAllTickets();
    TicketResponse? AssignTicket(int ticketId, AssignTicketRequest request);
    TicketResponse? StartProgressTicket(int ticketId, StartProgressRequest request);
    TicketResponse? ResolveTicket(int ticketId, ResolveTicketRequest request);
    TicketResponse? CloseTicket(int ticketId, CloseTicketRequest request);
    TicketResponse? CancelTicket(int ticketId, CancelTicketRequest request);
    TicketCommentResponse? AddComment(int ticketId, AddCommentRequest request);
    IReadOnlyCollection<TicketCommentResponse>? GetComments(int ticketId);
    IReadOnlyCollection<TicketHistoryItemResponse>? GetHistory(int ticketId);
}