using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Tickets;
using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Domain;
using SupportDesk.Domain.Users;

namespace SupportDesk.Infrastructure.Tickets;

public class EfTicketService : ITicketService
{
    private readonly SupportDeskDbContext _dbContext;

    public EfTicketService(SupportDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TicketResponse> CreateTicket(CreateTicketRequest request, Guid userId, CancellationToken cancellationToken)
    {
        var ticket = new Ticket(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            createdByUserId: userId);

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> GetTicketById(
        int ticketId, 
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId);
        
        query = ApplyVisibilityForRole(query, role, userId);
        
        var ticket = await query.FirstOrDefaultAsync(cancellationToken);

        if (ticket is null)
            return null;

        return MapToResponse(ticket);
    }

    public async Task<PagedResponse<TicketListItemResponse>> GetAllTickets(
        Guid userId,
        UserRole role,
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
        
        query = ApplyVisibilityForRole(query, role, userId);

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
    
    public async Task<TicketResponse?> AssignTicket(int ticketId, Guid actorId, UserRole role, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        if (role == UserRole.SupportAgent &&  request.AgentId != actorId)
            throw new DomainException("Support agent can assign ticket only to himself.");

        var query = _dbContext.Tickets
            .Where(ticket => ticket.Id == ticketId);
        
        query = ApplyVisibilityForRole(query, role, actorId);
        
        var ticket = await query.FirstOrDefaultAsync(cancellationToken);
        
        if (ticket is null)
            return null;

        var agentExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == request.AgentId && user.Role == UserRole.SupportAgent, cancellationToken);
        
        if (!agentExists)
            throw new DomainException("Support agent does not exist.");
        
        ticket.AssignTo(agentId: request.AgentId, actorId: actorId);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(ticket);

    }

    public Task<TicketResponse?> StartProgressTicket(int ticketId, Guid actorId, UserRole role, CancellationToken cancellationToken)
    {
        return ExecuteTicketChangeAsync(
            ticketId: ticketId,
            actorId: actorId,
            role: role,
            cancellationToken: cancellationToken,
            change: ticket => ticket.StartProgress(actorId: actorId));
    }

    public Task<TicketResponse?> ResolveTicket(int ticketId, Guid actorId, UserRole role, ResolveTicketRequest request, CancellationToken cancellationToken)
    {
        return ExecuteTicketChangeAsync(
            ticketId: ticketId,
            actorId: actorId,
            role: role,
            cancellationToken: cancellationToken,
            change: ticket => ticket.Resolve(actorId: actorId, resolution: request.Resolution));
    }

    public Task<TicketResponse?> CloseTicket(int ticketId, Guid actorId, UserRole role, CancellationToken cancellationToken)
    {
        return ExecuteTicketChangeAsync(
            ticketId: ticketId,
            actorId: actorId,
            role: role,
            cancellationToken: cancellationToken,
            change: ticket => ticket.Close(actorId: actorId));
    }

    public Task<TicketResponse?> CancelTicket(int ticketId, Guid actorId, UserRole role, CancelTicketRequest request, CancellationToken cancellationToken)
    {
       return ExecuteTicketChangeAsync(
            ticketId: ticketId,
            actorId: actorId,
            role: role,
            cancellationToken: cancellationToken,
            change: ticket => ticket.Cancel(actorId: actorId, reason: request.Reason));
    }

    public async Task<TicketCommentResponse?> AddComment(int ticketId, Guid actorId, UserRole role, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Tickets
            .Where(ticket => ticket.Id == ticketId);
        
        query = ApplyVisibilityForRole(query, role, actorId);
        
        var ticket = await query.FirstOrDefaultAsync(cancellationToken);

        if (ticket is null)
            return null;

        var comment = ticket.AddComment(authorId: actorId, text: request.CommentText);
        _dbContext.TicketComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToCommentResponse(comment);
    }

    public async Task<IReadOnlyCollection<TicketCommentResponse>?> GetComments(int ticketId, Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId);
        
        query = ApplyVisibilityForRole(query, role, userId);
        
        var visibleTicketExists = await query
            .AnyAsync(cancellationToken: cancellationToken);
        
        if (!visibleTicketExists)
            return null;

        var comments = await _dbContext.TicketComments
            .AsNoTracking()
            .Where(comment => comment.TicketId == ticketId)
            .OrderBy(comment => comment.CreatedAt)
            .Select(comment => new TicketCommentResponse(
                comment.Id,
                comment.TicketId,
                comment.AuthorUserId,
                comment.CommentText,
                comment.CreatedAt))
            .ToListAsync(cancellationToken: cancellationToken);

        return comments;
    }

    public async Task<IReadOnlyCollection<TicketHistoryItemResponse>?> GetHistory(int ticketId, Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId);
        
        query = ApplyVisibilityForRole(query, role, userId);
        
        var visibleTicketExists = await query
            .AnyAsync(cancellationToken: cancellationToken);
        
        if (!visibleTicketExists)
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

    private static IQueryable<Ticket> ApplyVisibilityForRole(IQueryable<Ticket> query, UserRole role, Guid userId)
    {
        return role switch
        {
            UserRole.Admin => query,

            UserRole.User => query.Where(ticket =>
                ticket.CreatedByUserId == userId),

            UserRole.SupportAgent => query.Where(ticket =>
                ticket.Status == TicketStatus.New ||
                ticket.AssignedAgentId == userId),

            _ => throw new DomainException("Unknown user role.")
        };
    }
    
    private async Task<TicketResponse?> ExecuteTicketChangeAsync(int ticketId, Guid actorId, UserRole role, CancellationToken cancellationToken, Action<Ticket> change)
    {
        var query = _dbContext.Tickets
            .Where(ticket => ticket.Id == ticketId);
        
        query = ApplyVisibilityForRole(query, role, actorId);
        
        var ticket = await query.FirstOrDefaultAsync(cancellationToken);

        if (ticket is null)
            return null;

        change(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        
        return MapToResponse(ticket);
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