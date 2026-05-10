using SupportDesk.Domain.Users;

namespace SupportDesk.Application.Auth;

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(User user);
}