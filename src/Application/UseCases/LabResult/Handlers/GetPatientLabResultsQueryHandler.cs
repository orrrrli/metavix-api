using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Queries;

namespace Application.UseCases.LabResult.Handlers;

internal sealed class GetPatientLabResultsQueryHandler
    : IRequestHandler<GetPatientLabResultsQuery, ErrorOr<List<LabResultResult>>>
{
    private readonly ILabResultRepository _labResultRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetPatientLabResultsQueryHandler(
        ILabResultRepository labResultRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _labResultRepository = labResultRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<LabResultResult>>> Handle(
        GetPatientLabResultsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Authorize
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Load — single query resolves ownership + existence.
        //    "Not found" and "not yours" are collapsed into Forbidden to
        //    close the patient-ID enumeration oracle.
        var patient = await _patientRepository.GetOwnedPatientAsync(
            request.PatientId, userId, cancellationToken);
        if (patient is null)
            return AuthErrors.Forbidden;

        var records = await _labResultRepository.GetAllByPatientIdAsync(request.PatientId);

        // 3. Map — an owned patient with no lab results yet is a valid empty
        //    result, not an error. Returning RecordsNotFound would force callers
        //    to treat "no results yet" as a failure.
        return records.Select(r => new LabResultResult(
            r.Id,
            r.PatientId,
            r.SampleDate,
            r.Hba1c,
            r.TotalCholesterol,
            r.Ldl,
            r.Hdl,
            r.Triglycerides,
            r.Creatinine,
            r.Bun,
            r.EgoProteins,
            r.EgoGlucose,
            r.Notes,
            r.CreatedAt)).ToList();
    }
}
