using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Mappers;
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
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        // The two reads are NOT redundant: step 1 is the auth gate (the caller
        // owns request.PatientId), this one fetches the record. Collapsing them
        // into a single `WHERE Id = @recordId AND PatientId = @patientId` would
        // drop the ownership check — the record row carries no UserId — and let
        // any authenticated user read another patient's record by guessing ids.
        var record = await _labResultRepository.GetByIdAsync(request.RecordId);

        if (record is null || record.PatientId != request.PatientId)
        {
            return RecordErrors.RecordNotFound;
        }

        return LabResultMapper.ToResult(record);
    }
}
