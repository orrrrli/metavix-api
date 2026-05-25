using Application.UseCases.DailyRecord.Common;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetLinkedPatientDailyRecordsQuery(
    Guid DoctorId,
    Guid PatientId) : IRequest<ErrorOr<List<DailyRecordResult>>>;
