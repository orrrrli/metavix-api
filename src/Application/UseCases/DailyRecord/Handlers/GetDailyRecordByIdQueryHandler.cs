using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Queries;

namespace Application.UseCases.DailyRecord.Handlers;

internal sealed class GetDailyRecordByIdQueryHandler
    : IRequestHandler<GetDailyRecordByIdQuery, ErrorOr<DailyRecordResult>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;

    public GetDailyRecordByIdQueryHandler(IDailyRecordRepository dailyRecordRepository)
    {
        _dailyRecordRepository = dailyRecordRepository;
    }

    public async Task<ErrorOr<DailyRecordResult>> Handle(
        GetDailyRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        var record = await _dailyRecordRepository.GetByIdAsync(request.RecordId);
        
        if (record is null)
        {
            return RecordErrors.RecordNotFound;
        }

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
