using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Domain;

namespace SupportDesk.Application.Tickets;

public sealed class InMemoryTicketService : ITicketService
{
    private readonly Dictionary<int, Ticket>  _tickets = new();
    private int _nextTicketId = 1;
    private readonly Dictionary<int, int> _nextCommentIds = new();
    
    public Task<TicketResponse> CreateTicket(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var id = _nextTicketId++;
        var ticket = new Ticket(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            createdByUserId: request.CreatedByUserId);
        
        _tickets.Add(id, ticket);

        return Task.FromResult<TicketResponse>(MapToResponse(ticket));
    }
    
    public Task<TicketResponse?> GetTicketById(int ticketId, CancellationToken cancellationToken)
    {
        if (!_tickets.TryGetValue(ticketId, out var ticket))
            return Task.FromResult<TicketResponse?>(null);
        
        return Task.FromResult<TicketResponse?>(MapToResponse(ticket));
    }

    public Task<PagedResponse<TicketListItemResponse>> GetAllTickets(
        TicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1)
            throw new DomainException("Page must be greater or equal to 1.");

        if (request.PageSize < 1 || request.PageSize > 100)
            throw new DomainException("Page size must be between 1 and 100.");

        IEnumerable<Ticket> query = _tickets.Values;

        if (request.Status is { Length: > 0 })
        {
            query = query.Where(ticket => request.Status.Contains(ticket.Status));
        }

        if (request.Priority is not null)
        {
            query = query.Where(ticket => ticket.Priority == request.Priority.Value);
        }

        query = (request.SortBy, request.SortDirection) switch
        {
            (TicketSortBy.CreatedAt, SortDirection.Ascending) =>
                query.OrderBy(ticket => ticket.CreatedAt),

            (TicketSortBy.CreatedAt, SortDirection.Descending) =>
                query.OrderByDescending(ticket => ticket.CreatedAt),

            (TicketSortBy.Priority, SortDirection.Ascending) =>
                query.OrderBy(ticket => ticket.Priority),

            (TicketSortBy.Priority, SortDirection.Descending) =>
                query.OrderByDescending(ticket => ticket.Priority),

            (TicketSortBy.Status, SortDirection.Ascending) =>
                query.OrderBy(ticket => ticket.Status),

            (TicketSortBy.Status, SortDirection.Descending) =>
                query.OrderByDescending(ticket => ticket.Status),

            _ => query.OrderByDescending(ticket => ticket.CreatedAt)
        };

        var totalCount = query.Count();

        var items = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(MapToListItemResponse)
            .ToList();

        var response = new PagedResponse<TicketListItemResponse>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Task.FromResult(response);
    }

    public Task<TicketResponse?> AssignTicket(int ticketId, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.AssignTo(request.AgentId, actorId: request.ActorId));
    }

    public Task<TicketResponse?> StartProgressTicket(int ticketId, StartProgressRequest request, CancellationToken cancellationToken)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.StartProgress(actorId: request.ActorId));
    }

    public Task<TicketResponse?> ResolveTicket(int ticketId, ResolveTicketRequest request, CancellationToken cancellationToken)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.Resolve(actorId: request.ActorId, resolution: request.Resolution));
    }

    public Task<TicketResponse?> CloseTicket(int ticketId, CloseTicketRequest request, CancellationToken cancellationToken)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.Close(actorId: request.ActorId));
    }

    public Task<TicketResponse?> CancelTicket(int ticketId, CancelTicketRequest request, CancellationToken cancellationToken)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.Cancel(actorId: request.ActorId, reason: request.Reason));
    }

    public Task<TicketCommentResponse?> AddComment(int ticketId, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var ticket = FindTicket(ticketId);
        if (ticket is null)
            return Task.FromResult<TicketCommentResponse?>(null);
        
        var commentId = GetNextCommentId(ticketId);
        var comment = ticket.AddComment(authorId: request.AuthorUserId, text: request.CommentText);

        return Task.FromResult<TicketCommentResponse?>(MapToCommentResponse(comment));
    }

    public Task<IReadOnlyCollection<TicketCommentResponse>?> GetComments(int ticketId, CancellationToken cancellationToken)
    {
        var ticket = FindTicket(ticketId);
        if (ticket is null)
            return Task.FromResult<IReadOnlyCollection<TicketCommentResponse>?>(null);

        var comments = ticket.Comments.Select(MapToCommentResponse).ToList();
        return Task.FromResult<IReadOnlyCollection<TicketCommentResponse>?>(comments);
    }

    public Task<IReadOnlyCollection<TicketHistoryItemResponse>?> GetHistory(int ticketId, CancellationToken cancellationToken)
    {
        var ticket = FindTicket(ticketId);
        if (ticket is null)
            return Task.FromResult<IReadOnlyCollection<TicketHistoryItemResponse>?>(null);

        var history = ticket.History.Select(MapToHistoryResponse).ToList();
        return Task.FromResult<IReadOnlyCollection<TicketHistoryItemResponse>?>(history);
    }

    private Ticket? FindTicket(int ticketId)
    {
        return _tickets.GetValueOrDefault(ticketId);
    }

    private int GetNextCommentId(int ticketId)
    {
        if (!_nextCommentIds.TryGetValue(ticketId, out var nextId))
            nextId = 1;

        _nextCommentIds[ticketId] = nextId + 1;

        return nextId;
    }

    private Task<TicketResponse?> ChangeTicket(int ticketId, Action<Ticket> change)
    {
        var ticket = FindTicket(ticketId);

        if (ticket is null)
            return Task.FromResult<TicketResponse?>(null);
        
        change(ticket);
        
        return Task.FromResult<TicketResponse?>(MapToResponse(ticket));
    }
    
    private static TicketListItemResponse MapToListItemResponse(Ticket ticket)
    {
        return new TicketListItemResponse(
            id: ticket.Id,
            title: ticket.Title,
            status: ticket.Status,
            priority: ticket.Priority,
            assignedAgentId: ticket.AssignedAgentId,
            createdAt: ticket.CreatedAt);
    }
    
    private static TicketResponse MapToResponse(Ticket ticket)
    {
        var response = new TicketResponse(
            id: ticket.Id,
            title: ticket.Title,
            description: ticket.Description,
            priority: ticket.Priority,
            createdByUserId: ticket.CreatedByUserId,
            status: ticket.Status,
            assignedAgentId: ticket.AssignedAgentId,
            createdAt: ticket.CreatedAt,
            updatedAt: ticket.UpdatedAt,
            resolvedAt: ticket.ResolvedAt,
            closedAt: ticket.ClosedAt);
        
        return response;
    }

    private static TicketCommentResponse MapToCommentResponse(TicketComment comment)
    {
        var response = new TicketCommentResponse(
            id: comment.Id,
            ticketId: comment.TicketId,
            authorUserId: comment.AuthorUserId,
            commentText: comment.CommentText,
            createdAt: comment.CreatedAt);
        
        return response;
    }

    private static TicketHistoryItemResponse MapToHistoryResponse(TicketHistoryItem historyItem)
    {
        var response = new TicketHistoryItemResponse(
            id: historyItem.Id,
            ticketId: historyItem.TicketId,
            actorUserId: historyItem.ActorUserId,
            action: historyItem.Action,
            oldStatus: historyItem.OldStatus,
            newStatus: historyItem.NewStatus,
            createdAt: historyItem.CreatedAt,
            details: historyItem.Details);

        return response;
    }
}