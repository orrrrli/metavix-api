using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Queries;

namespace Application.UseCases.DailyRecord.Handlers;

internal sealed class GetBodyStatsQueryHandler
    : IRequestHandler<GetBodyStatsQuery, ErrorOr<BodyStats>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetBodyStatsQueryHandler(
        IDailyRecordRepository dailyRecordRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _patientRepository     = patientRepository;
        _currentUser           = currentUser;
    }

    public async Task<ErrorOr<BodyStats>> Handle(
        GetBodyStatsQuery request,
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

        var record = await _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(request.PatientId, request.Date, cancellationToken);

        return new BodyStats(record?.WeightKg, record?.WaistCm);
    }
}
