using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Common;
using Application.UseCases.InsulinDm1.Queries;

namespace Application.UseCases.InsulinDm1.Handlers;

internal sealed class GetInsulinRecordsQueryHandler
    : IRequestHandler<GetInsulinRecordsQuery, ErrorOr<List<InsulinDm1RecordResult>>>
{
    private readonly IInsulinDm1Repository _insulinRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetInsulinRecordsQueryHandler(
        IInsulinDm1Repository insulinRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _insulinRepository = insulinRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<InsulinDm1RecordResult>>> Handle(
        GetInsulinRecordsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
            return PatientErrors.PatientsNotFound;

        var records = await _insulinRepository.GetRecordsByPatientIdAsync(request.PatientId);

        if (records.Count == 0)
            return InsulinDm1Errors.RecordsNotFound;

        return records.Select(r => new InsulinDm1RecordResult(
            r.Id,
            r.PatientId,
            r.RecordDate,
            r.GlucoseBefore,
            r.GlucoseAfter,
            r.TotalCarbs,
            r.DoseApplied,
            r.MealDescription,
            r.HowIFelt,
            r.CreatedAt)).ToList();
    }
}
