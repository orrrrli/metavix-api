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
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
        {
            return PatientErrors.PatientsNotFound;
        }

        var records = await _labResultRepository.GetAllByPatientIdAsync(request.PatientId);

        var results = records.Select(r => new LabResultResult(
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

        if (results.Count == 0)
        {
            return RecordErrors.RecordsNotFound;
        }

        return results;
    }
}
