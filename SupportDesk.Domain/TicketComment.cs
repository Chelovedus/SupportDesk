namespace SupportDesk.Domain;

public class TicketComment
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string CommentText { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    
    public TicketComment(int ticketId, Guid authorUserId, string commentText, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(commentText))
            throw new DomainException("Comment cannot be empty.");
        
        var normalizedText = commentText.Trim();
        
        TicketId = ticketId;
        AuthorUserId = authorUserId;
        CommentText = normalizedText;
        CreatedAt = createdAt;
    }
}