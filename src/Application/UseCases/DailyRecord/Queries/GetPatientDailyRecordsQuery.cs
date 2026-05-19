using Application.UseCases.DailyRecord.Common;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetPatientDailyRecordsQuery(
    Guid PatientId) : IRequest<ErrorOr<List<DailyRecordResult>>>;
