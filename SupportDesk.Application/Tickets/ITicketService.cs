using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;

namespace SupportDesk.Application.Tickets;

public interface ITicketService
{
    Task<TicketResponse> CreateTicket(CreateTicketRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> GetTicketById(int ticketId, CancellationToken cancellationToken);
    Task<PagedResponse<TicketListItemResponse>> GetAllTickets(TicketSearchRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> AssignTicket(int ticketId, AssignTicketRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> StartProgressTicket(int ticketId, StartProgressRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> ResolveTicket(int ticketId, ResolveTicketRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> CloseTicket(int ticketId, CloseTicketRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> CancelTicket(int ticketId, CancelTicketRequest request, CancellationToken cancellationToken);
    Task<TicketCommentResponse?> AddComment(int ticketId, AddCommentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TicketCommentResponse>?> GetComments(int ticketId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TicketHistoryItemResponse>?> GetHistory(int ticketId, CancellationToken cancellationToken);
}