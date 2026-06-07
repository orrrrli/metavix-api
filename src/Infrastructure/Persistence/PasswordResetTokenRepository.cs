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
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task MarkAsUsedAsync(PasswordResetToken token)
    {
        token.UsedAt = DateTime.UtcNow;
        _dbContext.PasswordResetTokens.Update(token);
        await _dbContext.SaveChangesAsync();
    }
}
