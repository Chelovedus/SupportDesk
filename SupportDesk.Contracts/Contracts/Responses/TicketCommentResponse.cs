namespace SupportDesk.Contracts.Responses;

public sealed class TicketCommentResponse
{
    public TicketCommentResponse(int id, int ticketId, int authorUserId, string commentText, DateTimeOffset createdAt)
    {
        Id = id;
        TicketId = ticketId;
        AuthorUserId = authorUserId;
        CommentText = commentText;
        CreatedAt = createdAt;
    }

    public int Id { get; set; }
    public int TicketId { get; set; }
    public int AuthorUserId { get; set; }
    public string CommentText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}