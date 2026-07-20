using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Mappers;
using Application.UseCases.DailyRecord.Queries;
using Domain.Enums;

namespace Application.UseCases.DailyRecord.Handlers;

internal sealed class GetPatientDailyRecordsQueryHandler
    : IRequestHandler<GetPatientDailyRecordsQuery, ErrorOr<List<DailyRecordResult>>>
{
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetPatientDailyRecordsQueryHandler(
        IDailyRecordRepository dailyRecordRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _dailyRecordRepository = dailyRecordRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<DailyRecordResult>>> Handle(
        GetPatientDailyRecordsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
        {
            return PatientErrors.PatientsNotFound;
        }

        bool hasRange = request.DateFrom.HasValue && request.DateTo.HasValue;

        var records = hasRange
            ? await _dailyRecordRepository.GetByPatientIdInRangeAsync(
                request.PatientId, request.DateFrom!.Value, request.DateTo!.Value, cancellationToken)
            : await _dailyRecordRepository.GetAllByPatientIdAsync(request.PatientId);

        var results = records.Select(DailyRecordMapper.ToResult).ToList();

        if (!hasRange && results.Count == 0)
        {
            return RecordErrors.RecordsNotFound;
        }

        return results;
    }
}
