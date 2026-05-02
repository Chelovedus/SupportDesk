namespace SupportDesk.Domain;

public class Ticket
{
    public Ticket(string title, string description, TicketPriority priority, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description cannot be empty.");
        
        var now = DateTimeOffset.UtcNow;
        
        Title = title;
        Description = description;
        Priority = priority;
        Status = TicketStatus.New;
        CreatedByUserId = createdByUserId;
        CreatedAt = now;
        UpdatedAt = now;
    }
    
    public int Id { get; private set; }
    public string Title { get; private set; }
    public string Description  { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public int CreatedByUserId { get; private set; }
    public int? AssignedAgentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    
    public IReadOnlyCollection<TicketHistoryItem> History => _history.AsReadOnly();
    public IReadOnlyCollection<TicketComment> Comments => _comments.AsReadOnly();

    private readonly List<TicketHistoryItem> _history = new();
    private readonly List<TicketComment> _comments = new();
    
    public void AssignTo(int agentId, int actorId)
    {
        if (Status != TicketStatus.New)
            throw new DomainException("Only new tickets can be assigned.");

        var now = DateTimeOffset.UtcNow;
        AssignedAgentId = agentId;
        ChangeStatus(
            newStatus: TicketStatus.Assigned,
            actorId: actorId,
            eventType: "TicketAssigned",
            description: $"Assigned to agent {agentId}",
            now: now);
    }

    public void StartProgress(int actorId)
    {
        if (Status is not (TicketStatus.Assigned or TicketStatus.Resolved))
            throw new DomainException("Only assigned or resolved tickets can be moved to in progress.");
        
        if (Status == TicketStatus.Resolved)
            ResolvedAt = null;
        
        var now = DateTimeOffset.UtcNow;
        ChangeStatus(
            newStatus: TicketStatus.InProgress,
            actorId: actorId,
            eventType: "TicketInProgress",
            description: $"In progress for actor {actorId}",
            now: now);
    }

    public void Resolve(int actorId, string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            throw new DomainException("Resolution cannot be empty.");
        if (Status != TicketStatus.InProgress)
            throw new DomainException("Only in progress tickets can be resolved.");
        
        var now = DateTimeOffset.UtcNow;
        ResolvedAt = now;
        ChangeStatus(
            newStatus: TicketStatus.Resolved,
            actorId: actorId,
            eventType: "TicketResolved",
            description: $"Resolved to actor {actorId}. Resolution: {resolution}",
            now: now);
    }

    public void Close(int actorId)
    {
        if (Status != TicketStatus.Resolved)
            throw new DomainException("Only resolved tickets can be closed.");
        
        var now = DateTimeOffset.UtcNow;
        ClosedAt = now;
        ChangeStatus(
            newStatus: TicketStatus.Closed,
            actorId: actorId,
            eventType: "TicketClosed",
            description: $"Closed by actor {actorId}",
            now: now);
    }

    public void Cancel(int actorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason cannot be empty.");
        if (Status is not (TicketStatus.New or TicketStatus.Assigned or TicketStatus.InProgress))
            throw new DomainException("Only new, assigned or in progress tickets can be cancelled.");
        
        var now = DateTimeOffset.UtcNow;
        ChangeStatus(
            newStatus: TicketStatus.Cancelled,
            actorId: actorId,
            eventType: "TicketCancelled",
            description: $"Cancelled by actor {actorId}. Reason: {reason}",
            now: now);
    }
    public void AddComment(int authorId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Comment cannot be empty.");
        
        var now = DateTimeOffset.UtcNow;
        _comments.Add(new TicketComment(
            ticketId: Id,
            authorUserId: authorId,
            commentText: text,
            createdAt: now));
        UpdatedAt = now;
    }

    private void ChangeStatus(
        TicketStatus newStatus,
        int actorId,
        string eventType,
        string description,
        DateTimeOffset now)
    {
        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = now;
        
        _history.Add(new TicketHistoryItem(
            action: eventType,
            details: description,
            ticketId: Id,
            actorUserId: actorId,
            oldStatus: oldStatus.ToString(),
            newStatus: newStatus.ToString(),
            createdAt: now));
    }


}