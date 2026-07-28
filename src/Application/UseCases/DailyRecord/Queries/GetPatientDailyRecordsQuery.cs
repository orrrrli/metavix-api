using Application.UseCases.DailyRecord.Common;
using Application.Common.Messaging;

namespace Application.UseCases.DailyRecord.Queries;

public sealed record GetPatientDailyRecordsQuery(
    Guid PatientId,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IQuery<ErrorOr<List<DailyRecordResult>>>;
