namespace SupportDesk.Contracts.Contracts.Responses;

public sealed class LoginResponse
{
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required string AccessToken { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}