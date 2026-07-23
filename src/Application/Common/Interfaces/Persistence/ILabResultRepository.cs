using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

/// <remarks>
/// CT-less methods (intentionally, for now — repository-wide pass deferred):
/// <list type="bullet">
///   <item><description><c>AddAsync</c></description></item>
///   <item><description><c>GetAllByPatientIdAsync</c></description></item>
///   <item><description><c>GetByIdAsync</c></description></item>
///   <item><description><c>GetLatestByPatientIdAsync</c></description></item>
/// </list>
/// TODO: trailing <c>CancellationToken cancellationToken</c>, no default. See
/// the remarks on <c>IPatientRepository</c> for the shared convention.
/// </remarks>
public interface ILabResultRepository
{
    Task AddAsync(LabResult result);
    Task<List<LabResult>> GetAllByPatientIdAsync(Guid patientId);
    Task<LabResult?> GetByIdAsync(Guid resultId);
    Task<LabResult?> GetLatestByPatientIdAsync(Guid patientId);
}
