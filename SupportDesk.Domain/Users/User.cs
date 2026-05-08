namespace SupportDesk.Domain.Users;

public sealed class User
{
    private User()
    {
        // For EF Core    
    }
    
    public User(Guid id, string displayName, string email, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new DomainException("User id can not be empty.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("User name can not be empty.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("User email can not be empty.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("User password hash can not be empty.");
        
        
        Id = id;
        DisplayName = displayName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsAgent()
    {
        return Role is UserRole.Admin or UserRole.SupportAgent;
    }

}