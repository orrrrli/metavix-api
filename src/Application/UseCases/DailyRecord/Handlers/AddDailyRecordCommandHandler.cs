using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Commands;
using Application.UseCases.DailyRecord.Common;

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

        var recordId = Guid.NewGuid();

        var glucoseReadings = request.GlucoseReadings?
            .Select(g => new Domain.Models.GlucoseReading
            {
                Id = Guid.NewGuid(),
                DailyRecordId = recordId,
                ReadingType = g.ReadingType,
                ValueMgDl = g.ValueMgDl,
                Time = g.Time,
                Foods = g.Foods,
                PostprandialWindow = g.PostprandialWindow
            }).ToList() ?? [];

        var record = new Domain.Models.DailyRecord
        {
            Id = recordId,
            PatientId = request.PatientId,
            RecordDate = request.RecordDate,
            RecordTime = request.RecordTime,
            SystolicPressure = request.SystolicPressure,
            DiastolicPressure = request.DiastolicPressure,
            HeartRate = request.HeartRate,
            WeightKg = request.WeightKg,
            WaistCm = request.WaistCm,
            Notes = request.Notes,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            GlucoseReadings = glucoseReadings
        };

        await _dailyRecordRepository.AddAsync(record);

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
            glucoseReadings.Select(g => new GlucoseReadingResult(
                g.Id, g.ReadingType, g.ValueMgDl, g.Time, g.Foods, g.PostprandialWindow)).ToList());
    }
}
