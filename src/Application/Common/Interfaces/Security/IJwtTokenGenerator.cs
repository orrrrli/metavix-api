using Domain.Models;

namespace Application.Common.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, string fullName);
}
