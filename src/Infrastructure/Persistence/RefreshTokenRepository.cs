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
    }

    // AsTracking: LoginCommandHandler/RefreshCommandHandler/LogoutCommandHandler
    // pass the returned token straight to RevokeAsync — see AppDbContext's
    // global QueryTrackingBehavior.NoTracking default.
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbContext.RefreshTokens
            .AsTracking()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);
    }

    public Task RevokeAsync(RefreshToken token)
    {
        token.IsRevoked = true;
        return Task.CompletedTask;
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        List<RefreshToken> tokens = await _dbContext.RefreshTokens
            .AsTracking()
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();

        foreach (RefreshToken token in tokens)
            token.IsRevoked = true;
    }
}
