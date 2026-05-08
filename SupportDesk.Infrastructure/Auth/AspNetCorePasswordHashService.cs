using Microsoft.AspNetCore.Identity;
using SupportDesk.Application.Auth;

namespace SupportDesk.Infrastructure.Auth;

public sealed class AspNetCorePasswordHashService : IPasswordHashService
{
    private static readonly object PasswordHasherUser = new();
    private readonly PasswordHasher<object> _passwordHasher = new();
    
    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(user: PasswordHasherUser, password: password);
    }

    public bool Verify(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user: PasswordHasherUser,
            hashedPassword: passwordHash,
            providedPassword: password);
        
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}