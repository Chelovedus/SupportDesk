using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;

namespace SupportDesk.Application.Tickets;

public interface ITicketService
{
    Task<TicketResponse> CreateTicket(CreateTicketRequest request);
    Task<TicketResponse?> GetTicketById(int ticketId);
    Task<IReadOnlyCollection<TicketListItemResponse>> GetAllTickets();
    Task<TicketResponse?> AssignTicket(int ticketId, AssignTicketRequest request);
    Task<TicketResponse?> StartProgressTicket(int ticketId, StartProgressRequest request);
    Task<TicketResponse?> ResolveTicket(int ticketId, ResolveTicketRequest request);
    Task<TicketResponse?> CloseTicket(int ticketId, CloseTicketRequest request);
    Task<TicketResponse?> CancelTicket(int ticketId, CancelTicketRequest request);
    Task<TicketCommentResponse?> AddComment(int ticketId, AddCommentRequest request);
    Task<IReadOnlyCollection<TicketCommentResponse>?> GetComments(int ticketId);
    Task<IReadOnlyCollection<TicketHistoryItemResponse>?> GetHistory(int ticketId);
}