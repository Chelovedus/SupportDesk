using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Tickets;
using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Domain;

namespace SupportDesk.Infrastructure.Tickets;

public class EfTicketService : ITicketService
{
    private readonly SupportDeskDbContext _dbContext;

    public EfTicketService(SupportDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TicketResponse> CreateTicket(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = new Ticket(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            createdByUserId: request.CreatedByUserId);

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> GetTicketById(int ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FindAsync(keyValues: [ticketId], cancellationToken: cancellationToken);

        if (ticket is null)
            return null;

        return MapToResponse(ticket);
    }

    public async Task<PagedResponse<TicketListItemResponse>> GetAllTickets(
        TicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1)
            throw new DomainException("Page must be greater or equal to 1.");
        if (request.PageSize < 1 || request.PageSize > 100)
            throw new DomainException("Page size must be between 1 and 100.");

        var query = _dbContext.Tickets
            .AsNoTracking()
            .AsQueryable();

        if (request.Status.Length > 0)
            query = query.Where(ticket => request.Status.Contains(ticket.Status));

        if (request.Priority is not null)
            query = query.Where(ticket => ticket.Priority == request.Priority.Value);

        var totalCount = await query.CountAsync(cancellationToken: cancellationToken);

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

        var skip = (request.Page - 1) * request.PageSize;

        var items = await query
            .Skip(skip)
            .Take(request.PageSize)
            .Select(ticket => new TicketListItemResponse(
                ticket.Id,
                ticket.Title,
                ticket.Status,
                ticket.Priority,
                ticket.CreatedAt,
                ticket.AssignedAgentId))
            .ToListAsync(cancellationToken: cancellationToken);

        return new PagedResponse<TicketListItemResponse>()
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TicketResponse?> AssignTicket(int ticketId, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FindAsync(keyValues: [ticketId], cancellationToken: cancellationToken);

        if (ticket is null)
            return null;

        ticket.AssignTo(agentId: request.AgentId, actorId: request.ActorId);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> StartProgressTicket(int ticketId, StartProgressRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FindAsync(keyValues: [ticketId], cancellationToken: cancellationToken);

        if (ticket is null)
            return null;

        ticket.StartProgress(actorId: request.ActorId);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> ResolveTicket(int ticketId, ResolveTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FindAsync(keyValues: [ticketId], cancellationToken: cancellationToken);

        if (ticket is null)
            return null;

        ticket.Resolve(actorId: request.ActorId, resolution: request.Resolution);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> CloseTicket(int ticketId, CloseTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FindAsync(keyValues: [ticketId], cancellationToken: cancellationToken);

        if (ticket is null)
            return null;

        ticket.Close(actorId: request.ActorId);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> CancelTicket(int ticketId, CancelTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FindAsync(keyValues: [ticketId], cancellationToken: cancellationToken);

        if (ticket is null)
            return null;

        ticket.Cancel(actorId: request.ActorId, reason: request.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
    }

    public async Task<TicketCommentResponse?> AddComment(int ticketId, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets.FindAsync(keyValues: [ticketId], cancellationToken: cancellationToken);

        if (ticket is null)
            return null;

        var comment = ticket.AddComment(authorId: request.AuthorUserId, text: request.CommentText);
        _dbContext.TicketComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToCommentResponse(comment);
    }

    public async Task<IReadOnlyCollection<TicketCommentResponse>?> GetComments(int ticketId, CancellationToken cancellationToken)
    {
        var ticketExists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(ticket => ticket.Id == ticketId, cancellationToken: cancellationToken);

        if (!ticketExists)
            return null;

        var comments = await _dbContext.TicketComments
            .AsNoTracking()
            .Where(comment => comment.TicketId == ticketId)
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new TicketCommentResponse(
                id: comment.Id,
                ticketId: comment.TicketId,
                authorUserId: comment.AuthorUserId,
                commentText: comment.CommentText,
                createdAt: comment.CreatedAt))
            .ToListAsync(cancellationToken: cancellationToken);

        return comments;
    }

    public async Task<IReadOnlyCollection<TicketHistoryItemResponse>?> GetHistory(int ticketId, CancellationToken cancellationToken)
    {
        var ticketExists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(ticket => ticket.Id == ticketId, cancellationToken: cancellationToken);

        if (!ticketExists)
            return null;

        var history = await _dbContext.TicketHistoryItems
            .AsNoTracking()
            .Where(history => history.TicketId == ticketId)
            .OrderBy(history => history.CreatedAt)
            .Select(history => new TicketHistoryItemResponse(
                id: history.Id,
                ticketId: history.TicketId,
                actorUserId: history.ActorUserId,
                action: history.Action,
                oldStatus: history.OldStatus,
                newStatus: history.NewStatus,
                createdAt: history.CreatedAt,
                details: history.Details))
            .ToListAsync(cancellationToken: cancellationToken);

        return history;
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
    
}