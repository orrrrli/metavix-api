using Application.UseCases.Patient.Common;

namespace Application.Common.Interfaces.Persistence;

public interface IPatientRepository
{
    /// <remarks>
    /// CT-less methods (intentionally, for now — repository-wide pass deferred):
    /// <list type="bullet">
    ///   <item><description><c>GetPatientByPatientId</c> — 1 call site: <c>PatientByIdQueryHandler.Handle</c>.</description></item>
    ///   <item><description><c>GetByIdAsync</c> — 3 call sites: <c>GetLinkedPatientProfileQueryHandler.Handle</c>, <c>AcceptLinkRequestCommandHandler.Handle</c>, <c>UnlinkPatientCommandHandler.Handle</c>.</description></item>
    ///   <item><description><c>UpdateAsync</c> — 4 call sites: <c>UpdatePatientProfileCommandHandler.Handle</c>, <c>AcceptLinkRequestCommandHandler.Handle</c>, <c>UnlinkPatientCommandHandler.Handle</c>, <c>RevokeDoctorAccessCommandHandler.Handle</c>.</description></item>
    /// </list>
    /// When the pass that retires this remark lands, every method above should
    /// take a trailing <c>CancellationToken cancellationToken</c> (no default —
    /// matches <c>GetByUserIdAsync</c> / <c>GetOwnedPatientAsync</c> below, which
    /// force callers to propagate the request token). The handler call sites
    /// already have a <c>cancellationToken</c> in scope, so propagation is
    /// mechanical.
    /// Sibling repositories carry the same gap:
    /// <c>IDailyRecordRepository.GetAllByPatientIdAsync</c>, <c>GetByIdAsync</c>,
    /// <c>GetLatestByPatientIdAsync</c> (5 call sites across
    /// EvaluateGoalsCommandHandler, GetLinkedPatientDailyRecordsQueryHandler,
    /// GetPatientResumenQueryHandler, GetDailyRecordByIdQueryHandler,
    /// GetPatientDailyRecordsQueryHandler);
    /// <c>ILabResultRepository.AddAsync</c>, <c>GetAllByPatientIdAsync</c>,
    /// <c>GetByIdAsync</c>, <c>GetLatestByPatientIdAsync</c> (6 call sites across
    /// GetPatientLabResultsQueryHandler, GetLabResultByIdQueryHandler,
    /// AddLabResultCommandHandler, EvaluateGoalsCommandHandler,
    /// GetLinkedPatientLabResultsQueryHandler, GetPatientResumenQueryHandler).
    /// They should be swept in the same PR.
    /// </remarks>
    Task<PatientResult?> GetPatientByPatientId(Guid patientId);
    Task<Domain.Models.Patient?> GetByIdAsync(Guid patientId);
    Task UpdateAsync(Domain.Models.Patient patient);
    Task<Guid?> GetPatientIdByUserIdAsync(Guid userId);

    /// <remarks>
    /// For "my profile" endpoints: the caller acts on their own patient record,
    /// so a single by-userId lookup is the right granularity — no patientId is
    /// supplied by the request. A null result means the authenticated user has
    /// no patient profile yet, which is a missing resource, not a permissions
    /// failure (callers should surface PatientNotFound, not Forbidden).
    /// </remarks>
    Task<Domain.Models.Patient?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<Domain.Models.Patient?> GetOwnedPatientAsync(
        Guid patientId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<bool> ExistsByMedicalRecordNumberAsync(string medicalRecordNumber, CancellationToken cancellationToken = default);
}
