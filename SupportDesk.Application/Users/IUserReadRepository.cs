using SupportDesk.Domain.Users;

namespace SupportDesk.Application.Users;

public interface IUserReadRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}