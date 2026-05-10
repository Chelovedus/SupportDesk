namespace SupportDesk.Contracts.Requests;

public sealed class AddCommentRequest
{
    public required string CommentText { get; set; }
}