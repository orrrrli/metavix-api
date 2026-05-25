using Application.Common.Interfaces.Persistence;
using Domain.Models;

namespace Infrastructure.Persistence;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _dbContext.RefreshTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);
    }

    public async Task RevokeAsync(RefreshToken token)
    {
        token.IsRevoked = true;
        _dbContext.RefreshTokens.Update(token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        List<RefreshToken> tokens = await _dbContext.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();

        foreach (RefreshToken token in tokens)
            token.IsRevoked = true;

        await _dbContext.SaveChangesAsync();
    }
}
