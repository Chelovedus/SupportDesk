using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Domain;

namespace SupportDesk.Application.Tickets;

public sealed class InMemoryTicketService : ITicketService
{
    private readonly Dictionary<int, Ticket>  _tickets = new();
    private int _nextTicketId = 1;
    private readonly Dictionary<int, int> _nextCommentIds = new();
    
    public TicketResponse CreateTicket(CreateTicketRequest request)
    {
        var id = _nextTicketId++;
        var ticket = new Ticket(
            id: id,
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            createdByUserId: request.CreatedByUserId);
        
        _tickets.Add(id, ticket);

        return MapToResponse(ticket);
    }
    
    public TicketResponse? GetTicketById(int ticketId)
    {
        var ticket = FindTicket(ticketId);
        return ticket is not null ? MapToResponse(ticket) : null;
    }

    public IReadOnlyCollection<TicketListItemResponse> GetAllTickets()
    {
        return _tickets.Values.Select(MapToListItemResponse).ToList();
    }

    public TicketResponse? AssignTicket(int ticketId, AssignTicketRequest request)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.AssignTo(request.AgentId, actorId: request.ActorId));
    }

    public TicketResponse? StartProgressTicket(int ticketId, StartProgressRequest request)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.StartProgress(actorId: request.ActorId));
    }

    public TicketResponse? ResolveTicket(int ticketId, ResolveTicketRequest request)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.Resolve(actorId: request.ActorId, resolution: request.Resolution));
    }

    public TicketResponse? CloseTicket(int ticketId, CloseTicketRequest request)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.Close(actorId: request.ActorId));
    }

    public TicketResponse? CancelTicket(int ticketId, CancelTicketRequest request)
    {
        return ChangeTicket(
            ticketId: ticketId,
            change: ticket => ticket.Cancel(actorId: request.ActorId, reason: request.Reason));
    }

    public TicketCommentResponse? AddComment(int ticketId, AddCommentRequest request)
    {
        var ticket = FindTicket(ticketId);
        if (ticket is null)
            return null;
        
        var commentId = GetNextCommentId(ticketId);
        var comment = ticket.AddComment(commentId: commentId, authorId: request.AuthorUserId, text: request.CommentText);

        return MapToCommentResponse(comment);
    }

    public IReadOnlyCollection<TicketCommentResponse>? GetComments(int ticketId)
    {
        var ticket = FindTicket(ticketId);
        if (ticket is null)
            return null;

        var comments = ticket.Comments.Select(MapToCommentResponse).ToList();
        return comments;
    }

    public IReadOnlyCollection<TicketHistoryItemResponse>? GetHistory(int ticketId)
    {
        var ticket = FindTicket(ticketId);
        if (ticket is null)
            return null;

        var history = ticket.History.Select(MapToHistoryResponse).ToList();
        return history;
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

    private TicketResponse? ChangeTicket(int ticketId, Action<Ticket> change)
    {
        var ticket = FindTicket(ticketId);

        if (ticket is null)
            return null;
        
        change(ticket);
        
        return MapToResponse(ticket);
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