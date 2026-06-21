using Application.UseCases.DailyRecord.Common;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetPatientDailyRecordsQuery(
    Guid PatientId,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IRequest<ErrorOr<List<DailyRecordResult>>>;
