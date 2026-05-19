using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Queries;

namespace Application.UseCases.LabResult.Handlers;

internal sealed class GetLabResultByIdQueryHandler
    : IRequestHandler<GetLabResultByIdQuery, ErrorOr<LabResultResult>>
{
    private readonly ILabResultRepository _labResultRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetLabResultByIdQueryHandler(
        ILabResultRepository labResultRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _labResultRepository = labResultRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<LabResultResult>> Handle(
        GetLabResultByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var record = await _labResultRepository.GetByIdAsync(request.RecordId);

        if (record is null || record.PatientId != request.PatientId)
        {
            return RecordErrors.RecordNotFound;
        }

        return new LabResultResult(
            record.Id,
            record.PatientId,
            record.SampleDate,
            record.Hba1c,
            record.TotalCholesterol,
            record.Ldl,
            record.Hdl,
            record.Triglycerides,
            record.Creatinine,
            record.Bun,
            record.EgoProteins,
            record.EgoGlucose,
            record.Notes,
            record.CreatedAt);
    }
}
