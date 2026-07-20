using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Queries;

namespace Application.UseCases.DailyRecord.Handlers;

internal sealed class GetDailyRecordSnapshotQueryHandler
    : IRequestHandler<GetDailyRecordSnapshotQuery, ErrorOr<DailyRecordSnapshot>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetDailyRecordSnapshotQueryHandler(
        IDailyRecordRepository dailyRecordRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _patientRepository     = patientRepository;
        _currentUser           = currentUser;
    }

    public async Task<ErrorOr<DailyRecordSnapshot>> Handle(
        GetDailyRecordSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var record = await _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(request.PatientId, request.Date, cancellationToken);

        return new DailyRecordSnapshot(record?.WeightKg, record?.WaistCm);
    }
}
