using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Auth;
using SupportDesk.Domain.Users;

namespace SupportDesk.Infrastructure.Seed;

public class DatabaseSeeder
{
    private readonly SupportDeskDbContext _dbContext;
    private readonly IPasswordHashService  _passwordHashService;

    public DatabaseSeeder(
        SupportDeskDbContext dbContext,
        IPasswordHashService aspNetCorePasswordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = aspNetCorePasswordHashService;
    }
    
    public async Task AddSeedUsersAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }
        
        var now = DateTimeOffset.UtcNow;
        
        var user = new User(
            id: Guid.CreateVersion7(),
            displayName: "User",
            email: "user@example.com",
            passwordHash: _passwordHashService.Hash("Password123!"),
            role: UserRole.User,
            createdAt: now);
        
        var userSecond = new User(
            id: Guid.CreateVersion7(),
            displayName: "UserSecond",
            email: "usersecond@example.com",
            passwordHash: _passwordHashService.Hash("Password123!"),
            role: UserRole.User,
            createdAt: now);
        
        var agent = new User(
            id: Guid.CreateVersion7(),
            displayName: "SupportAgent",
            email: "agent@example.com",
            passwordHash: _passwordHashService.Hash("Password123!"),
            role: UserRole.SupportAgent,
            createdAt: now);
        
        var admin = new User(
            id: Guid.CreateVersion7(),
            displayName: "Admin",
            email: "admin@example.com",
            passwordHash: _passwordHashService.Hash("Password123!"),
            role: UserRole.Admin,
            createdAt: now);
        
        _dbContext.Users.AddRange(user, userSecond, agent, admin);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}