using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Queries;
using Domain.Enums;

namespace Application.UseCases.DailyRecord.Handlers;

internal sealed class GetDailyRecordByIdQueryHandler
    : IRequestHandler<GetDailyRecordByIdQuery, ErrorOr<DailyRecordResult>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetDailyRecordByIdQueryHandler(
        IDailyRecordRepository dailyRecordRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<DailyRecordResult>> Handle(
        GetDailyRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var record = await _dailyRecordRepository.GetByIdAsync(request.RecordId);

        if (record is null || record.PatientId != request.PatientId)
        {
            return RecordErrors.RecordNotFound;
        }

        var glucoseReadings = record.GlucoseReadings
            .Select(g => new GlucoseReadingResult(
                g.Id, g.ReadingType, g.ValueMgDl, g.Time, g.Foods, g.PostprandialWindow)).ToList();

        return new DailyRecordResult(
            record.Id,
            record.PatientId,
            record.RecordDate,
            record.RecordTime,
            record.SystolicPressure,
            record.DiastolicPressure,
            record.HeartRate,
            record.WeightKg,
            record.WaistCm,
            record.Notes,
            record.CreatedAt,
            glucoseReadings);
    }
}
