using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Domain.Users;

namespace SupportDesk.Application.Tickets;

public interface ITicketService
{
    Task<TicketResponse> CreateTicket(CreateTicketRequest request, Guid userId, CancellationToken cancellationToken);
    Task<TicketResponse?> GetTicketById(int ticketId, Guid userId, UserRole role, CancellationToken cancellationToken);
    Task<PagedResponse<TicketListItemResponse>> GetAllTickets(Guid userId, UserRole role, TicketSearchRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> AssignTicket(int ticketId, Guid actorId, UserRole role, AssignTicketRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> StartProgressTicket(int ticketId, Guid actorId, UserRole role, CancellationToken cancellationToken);
    Task<TicketResponse?> ResolveTicket(int ticketId, Guid actorId, UserRole role, ResolveTicketRequest request, CancellationToken cancellationToken);
    Task<TicketResponse?> CloseTicket(int ticketId, Guid actorId, UserRole role, CancellationToken cancellationToken);
    Task<TicketResponse?> CancelTicket(int ticketId, Guid actorId, UserRole role, CancelTicketRequest request, CancellationToken cancellationToken);
    Task<TicketCommentResponse?> AddComment(int ticketId, Guid actorId, UserRole role, AddCommentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TicketCommentResponse>?> GetComments(int ticketId, Guid userId, UserRole role, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TicketHistoryItemResponse>?> GetHistory(int ticketId, Guid userId, UserRole role, CancellationToken cancellationToken);
}