using SupportDesk.Application.Users;
using SupportDesk.Contracts.Contracts.Requests;
using SupportDesk.Contracts.Contracts.Responses;
using SupportDesk.Domain;

namespace SupportDesk.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserReadRepository _userReadRepository;
    private readonly IPasswordHashService _passwordHashService;

    public AuthService(IUserReadRepository userReadRepository, IPasswordHashService passwordHashService)
    {
        _userReadRepository = userReadRepository;
        _passwordHashService = passwordHashService;
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
        
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        return new LoginResponse()
        {
            Email = email,
            AccessToken = "WIP_ACCESS_TOKEN",
            Role = user.Role.ToString(),
            ExpiresAt = expiresAt
        };

    }
}