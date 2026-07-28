using Application.UseCases.DailyRecord.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetLinkedPatientDailyRecordsQuery(
    Guid DoctorId,
    Guid PatientId) : IQuery<ErrorOr<List<DailyRecordResult>>>;
