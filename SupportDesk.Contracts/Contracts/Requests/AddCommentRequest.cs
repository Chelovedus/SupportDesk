namespace SupportDesk.Contracts.Requests;

public sealed class AddCommentRequest
{
    public int AuthorUserId { get; set; }
    public required string CommentText { get; set; }
}