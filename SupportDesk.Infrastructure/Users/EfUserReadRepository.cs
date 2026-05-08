using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Users;
using SupportDesk.Domain.Users;

namespace SupportDesk.Infrastructure.Users;

public class EfUserReadRepository : IUserReadRepository
{
    private readonly SupportDeskDbContext _dbContext;

    public EfUserReadRepository(SupportDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }
        
    
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
    }
}