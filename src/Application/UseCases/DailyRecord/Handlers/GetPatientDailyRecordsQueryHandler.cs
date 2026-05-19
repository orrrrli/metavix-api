using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Queries;

namespace Application.UseCases.DailyRecord.Handlers;

internal sealed class GetPatientDailyRecordsQueryHandler
    : IRequestHandler<GetPatientDailyRecordsQuery, ErrorOr<List<DailyRecordResult>>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IPatientRepository _patientRepository;

    public GetPatientDailyRecordsQueryHandler(
        IDailyRecordRepository dailyRecordRepository,
        IPatientRepository patientRepository)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _patientRepository = patientRepository;
    }

    public async Task<ErrorOr<List<DailyRecordResult>>> Handle(
        GetPatientDailyRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
        {
            return PatientErrors.PatientsNotFound;
        }

        var records = await _dailyRecordRepository.GetAllByPatientIdAsync(request.PatientId);

        var results = records.Select(r => new DailyRecordResult(
            r.Id,
            r.PatientId,
            r.RecordDate,
            r.RecordTime,
            r.SystolicPressure,
            r.DiastolicPressure,
            r.HeartRate,
            r.WeightKg,
            r.WaistCm,
            r.Notes,
            r.CreatedAt)).ToList();

        if (results.Count == 0)
        {
            return RecordErrors.RecordsNotFound;
        }

        return results;
    }
}
