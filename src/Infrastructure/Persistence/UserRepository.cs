using Application.Common.Interfaces.Persistence;

namespace Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Domain.Models.User?> GetByEmailAsync(string email)
    {
        return await _dbContext.Users
            .Include(u => u.Doctor)
            .Include(u => u.Patient)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    // AsTracking: ResetPasswordCommandHandler mutates the returned user and
    // passes it to UpdateAsync — see AppDbContext's global
    // QueryTrackingBehavior.NoTracking default.
    public async Task<Domain.Models.User?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Users
            .AsTracking()
            .Include(u => u.Doctor)
            .Include(u => u.Patient)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _dbContext.Users.AnyAsync(u => u.Email == email);
    }

    public async Task AddAsync(Domain.Models.User user)
    {
        await _dbContext.Users.AddAsync(user);
    }

    public Task UpdateAsync(Domain.Models.User user) => Task.CompletedTask;
}
