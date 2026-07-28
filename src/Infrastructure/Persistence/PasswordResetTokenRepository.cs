using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _dbContext;

    public PasswordResetTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PasswordResetToken token)
    {
        await _dbContext.PasswordResetTokens.AddAsync(token);
    }

    // AsTracking: ResetPasswordCommandHandler passes the returned token
    // straight to MarkAsUsedAsync — see AppDbContext's global
    // QueryTrackingBehavior.NoTracking default.
    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _dbContext.PasswordResetTokens
            .AsTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public Task MarkAsUsedAsync(PasswordResetToken token)
    {
        token.UsedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
