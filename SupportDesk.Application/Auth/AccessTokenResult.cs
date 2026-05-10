namespace SupportDesk.Application.Auth;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt);