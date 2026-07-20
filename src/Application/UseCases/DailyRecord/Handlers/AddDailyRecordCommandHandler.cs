using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Commands;
using Application.UseCases.DailyRecord.Common;
using DomainDailyRecord = Domain.Models.DailyRecord;
using DomainGlucoseReading = Domain.Models.GlucoseReading;

namespace Application.UseCases.DailyRecord.Handlers;

internal sealed class AddDailyRecordCommandHandler
    : IRequestHandler<AddDailyRecordCommand, ErrorOr<DailyRecordResult>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddDailyRecordCommandHandler(
        IDailyRecordRepository dailyRecordRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<DailyRecordResult>> Handle(
        AddDailyRecordCommand request,
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
        if (!patient.IsActive)
            return RecordErrors.InactivePatient;

        // 3. Execute domain operation. The factory enforces clinical
        //    invariants (BP pair, glucose range, time required) and
        //    returns ErrorOr — the handler stays free of validation.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var createResult = DomainDailyRecord.Create(
            patientId: request.PatientId,
            recordDate: request.RecordDate,
            recordTime: request.RecordTime,
            systolicPressure: request.SystolicPressure,
            diastolicPressure: request.DiastolicPressure,
            heartRate: request.HeartRate,
            weightKg: request.WeightKg,
            waistCm: request.WaistCm,
            notes: request.Notes,
            now: now);
        if (createResult.IsError)
            return createResult.Errors;

        var record = createResult.Value;

        // 4. Attach glucose readings, if any. The factory on each
        //    GlucoseReading enforces the per-reading invariants.
        if (request.GlucoseReadings is { Count: > 0 })
        {
            foreach (var g in request.GlucoseReadings)
            {
                var readingResult = DomainGlucoseReading.Create(
                    dailyRecordId: record.Id,
                    type: g.ReadingType,
                    valueMgDl: g.ValueMgDl,
                    time: g.Time,
                    foods: g.Foods,
                    postprandialWindow: g.PostprandialWindow,
                    now: now);
                if (readingResult.IsError)
                    return readingResult.Errors;
                record.GlucoseReadings.Add(readingResult.Value);
            }
        }

        // 5. Persist
        await _dailyRecordRepository.AddAsync(record, cancellationToken);

        // 6. Return result
        return MapToResult(record);
    }

    private static DailyRecordResult MapToResult(DomainDailyRecord record) => new(
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
        record.GlucoseReadings
            .Select(g => new GlucoseReadingResult(
                g.Id, g.ReadingType, g.ValueMgDl, g.Time, g.Foods, g.PostprandialWindow))
            .ToList());
}
