using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.Doctor.Queries;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetLinkedPatientDailyRecordsQueryHandler
    : IRequestHandler<GetLinkedPatientDailyRecordsQuery, ErrorOr<List<DailyRecordResult>>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly ICurrentUserService _currentUser;

    public GetLinkedPatientDailyRecordsQueryHandler(
        IDailyRecordRepository dailyRecordRepository,
        IDoctorRepository doctorRepository,
        IPatientDoctorRequestRepository requestRepository,
        ICurrentUserService currentUser)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _doctorRepository = doctorRepository;
        _requestRepository = requestRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<DailyRecordResult>>> Handle(
        GetLinkedPatientDailyRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var authError = await DoctorPatientLinkAuth.AuthorizeAsync(
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId);
        if (authError is not null)
            return authError.Value;

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
            r.CreatedAt,
            r.GlucoseReadings.Select(g => new GlucoseReadingResult(
                g.Id, g.ReadingType, g.ValueMgDl, g.Time, g.Foods)).ToList())).ToList();

        if (results.Count == 0)
            return RecordErrors.RecordsNotFound;

        return results;
    }
}
