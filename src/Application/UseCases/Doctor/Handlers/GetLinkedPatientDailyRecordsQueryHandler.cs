using Application.Common.Authorization;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Mappers;
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
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId, cancellationToken);
        if (authError is not null)
            return authError.Value;

        var records = await _dailyRecordRepository.GetAllByPatientIdAsync(request.PatientId);

        // Mirrors GetPatientDailyRecordsQueryHandler (empty list is not an error).
        return records.Select(DailyRecordMapper.ToResult).ToList();
    }
}
