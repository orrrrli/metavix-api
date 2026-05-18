namespace Application.Common.Interfaces.Services;

public interface IDatabaseValidator
{
    Task ValidateAsync(CancellationToken cancellationToken = default);
}
