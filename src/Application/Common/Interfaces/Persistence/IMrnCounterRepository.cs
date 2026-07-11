namespace Application.Common.Interfaces.Persistence;

/// <summary>
/// Reads the highest in-use MRN sequence for a given calendar year so the
/// <see cref="Application.Common.Generators.MrnGenerator"/> can suggest the
/// next value. Not a strict counter — the application-level
/// <c>ExistsByMedicalRecordNumberAsync</c> check and the database unique
/// index are the actual enforcement.
/// </summary>
public interface IMrnCounterRepository
{
    /// <summary>
    /// Returns the highest 6-digit numeric suffix currently in use for MRNs
    /// prefixed with <c>MRN-{year}-</c>, or <c>0</c> if no such MRNs exist.
    /// </summary>
    Task<int> GetMaxSequenceForYearAsync(int year, CancellationToken cancellationToken = default);
}
