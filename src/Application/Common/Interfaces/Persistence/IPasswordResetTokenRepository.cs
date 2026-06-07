using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
    Task MarkAsUsedAsync(PasswordResetToken token);
}
