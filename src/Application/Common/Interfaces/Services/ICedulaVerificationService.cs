namespace Application.Common.Interfaces.Services;

public interface ICedulaVerificationService
{
    Task<bool> VerifyAsync(string licenseNumber, CancellationToken cancellationToken = default);
}
