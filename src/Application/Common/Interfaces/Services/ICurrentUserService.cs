namespace Application.Common.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
}
