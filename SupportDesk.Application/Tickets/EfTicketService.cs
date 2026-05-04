using Microsoft.EntityFrameworkCore;
using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

namespace SupportDesk.Application.Tickets;

public class EfTicketService : ITicketService
{
    private readonly SupportDeskDbContext _dbContext;

    public EfTicketService(SupportDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TicketResponse> CreateTicket(CreateTicketRequest request)
    {
        var ticket = new Ticket(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            createdByUserId: request.CreatedByUserId);

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> GetTicketById(int ticketId)
    {
        var ticket = _dbContext.Tickets.Find(ticketId);

        if (ticket is null)
            return null;

        return MapToResponse(ticket);
    }

    public async Task<IReadOnlyCollection<TicketListItemResponse>> GetAllTickets()
    {
        var tickets = await _dbContext.Tickets
            .AsNoTracking()
            .Select(ticket => new TicketListItemResponse(
                ticket.Id,
                ticket.Title,
                ticket.Status,
                ticket.Priority,
                ticket.CreatedAt,
                ticket.AssignedAgentId))
            .ToListAsync();

        return tickets;
    }

    public async Task<TicketResponse?> AssignTicket(int ticketId, AssignTicketRequest request)
    {
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);

        if (ticket is null)
            return null;

        ticket.AssignTo(agentId: request.AgentId, actorId: request.ActorId);
        await _dbContext.SaveChangesAsync();
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> StartProgressTicket(int ticketId, StartProgressRequest request)
    {
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);

        if (ticket is null)
            return null;

        ticket.StartProgress(actorId: request.ActorId);
        await _dbContext.SaveChangesAsync();
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> ResolveTicket(int ticketId, ResolveTicketRequest request)
    {
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);

        if (ticket is null)
            return null;

        ticket.Resolve(actorId: request.ActorId, resolution: request.Resolution);
        await _dbContext.SaveChangesAsync();
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> CloseTicket(int ticketId, CloseTicketRequest request)
    {
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);

        if (ticket is null)
            return null;

        ticket.Close(actorId: request.ActorId);
        await _dbContext.SaveChangesAsync();
        
        return MapToResponse(ticket);
    }

    public async Task<TicketResponse?> CancelTicket(int ticketId, CancelTicketRequest request)
    {
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);

        if (ticket is null)
            return null;

        ticket.Cancel(actorId: request.ActorId, reason: request.Reason);
        await _dbContext.SaveChangesAsync();
        
        return MapToResponse(ticket);
    }

    public async Task<TicketCommentResponse?> AddComment(int ticketId, AddCommentRequest request)
    {
        var ticket = await _dbContext.Tickets.FindAsync(ticketId);

        if (ticket is null)
            return null;

        var comment = ticket.AddComment(authorId: request.AuthorUserId, text: request.CommentText);
        _dbContext.TicketComments.Add(comment);
        await _dbContext.SaveChangesAsync();
        
        return MapToCommentResponse(comment);
    }

    public async Task<IReadOnlyCollection<TicketCommentResponse>?> GetComments(int ticketId)
    {
        var ticketExists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(ticket => ticket.Id == ticketId);

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
            .ToListAsync();

        return comments;
    }

    public async Task<IReadOnlyCollection<TicketHistoryItemResponse>?> GetHistory(int ticketId)
    {
        var ticketExists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(ticket => ticket.Id == ticketId);

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
            .ToListAsync();

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