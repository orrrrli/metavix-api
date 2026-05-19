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
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddDailyRecordCommandHandler(
        IDailyRecordRepository dailyRecordRepository,
        IPatientRepository patientRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _patientRepository = patientRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<DailyRecordResult>> Handle(
        AddDailyRecordCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
        {
            return PatientErrors.PatientsNotFound;
        }

        var record = new Domain.Models.DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            RecordDate = request.RecordDate,
            RecordTime = request.RecordTime,
            SystolicPressure = request.SystolicPressure,
            DiastolicPressure = request.DiastolicPressure,
            HeartRate = request.HeartRate,
            WeightKg = request.WeightKg,
            WaistCm = request.WaistCm,
            Notes = request.Notes,
            CreatedAt = _dateTimeProvider.UtcNow
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
            record.CreatedAt);
    }
}
