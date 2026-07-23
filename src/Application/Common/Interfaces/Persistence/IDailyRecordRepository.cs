using Domain.Models;

namespace Application.Common.Interfaces.Persistence;

/// <remarks>
/// CT-less methods (intentionally, for now — repository-wide pass deferred):
/// <list type="bullet">
///   <item><description><c>GetAllByPatientIdAsync</c></description></item>
///   <item><description><c>GetByIdAsync</c></description></item>
///   <item><description><c>GetLatestByPatientIdAsync</c></description></item>
/// </list>
/// TODO: trailing <c>CancellationToken cancellationToken</c>, no default. See
/// the remarks on <c>IPatientRepository</c> for the shared convention.
/// </remarks>
public interface IDailyRecordRepository
{
    Task AddAsync(DailyRecord record, CancellationToken cancellationToken = default);
    Task<List<DailyRecord>> GetAllByPatientIdAsync(Guid patientId);
    Task<DailyRecord?> GetByIdAsync(Guid recordId);
    Task<DailyRecord?> GetLatestByPatientIdAsync(Guid patientId);
    Task<DailyRecord?> GetFirstByPatientIdAndDateAsync(Guid patientId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<DailyRecord>> GetByPatientIdInRangeAsync(Guid patientId, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default);
}
