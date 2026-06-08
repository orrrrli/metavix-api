namespace Application.Common.Interfaces.Services;

public interface ICedulaVerificationService
{
    // Returns null when the cédula is not found or the scraper is unreachable.
    Task<CedulaVerificationResult?> VerifyAsync(string licenseNumber, CancellationToken cancellationToken = default);
}
