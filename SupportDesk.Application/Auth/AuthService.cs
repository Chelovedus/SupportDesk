using SupportDesk.Application.Users;
using SupportDesk.Contracts.Contracts.Requests;
using SupportDesk.Contracts.Contracts.Responses;
using SupportDesk.Domain;

namespace SupportDesk.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserReadRepository _userReadRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUserReadRepository userReadRepository, IPasswordHashService passwordHashService, IJwtTokenService jwtTokenService)
    {
        _userReadRepository = userReadRepository;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
    }
    public async Task<LoginResponse> TryLogin(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email;
        var user = await _userReadRepository.GetByEmailAsync(email: email, cancellationToken: cancellationToken);

        if (user == null)
            throw new DomainException("Invalid email or password.");

        var password = request.Password;
        var hash = user.PasswordHash;

        bool isSuccess = _passwordHashService.Verify(password: password, passwordHash: hash);

        if (isSuccess is false)
            throw new DomainException("Invalid email or password.");
        
        var accessToken = _jwtTokenService.CreateAccessToken(user);

        return new LoginResponse()
        {
            Email = email,
            UserId = user.Id,
            AccessToken = accessToken.AccessToken,
            Role = user.Role.ToString(),
            ExpiresAt = accessToken.ExpiresAt
        };

    }
}