using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Common;

namespace Application.UseCases.InsulinDm1.Handlers;

internal sealed class UpsertInsulinProfileCommandHandler
    : IRequestHandler<UpsertInsulinProfileCommand, ErrorOr<InsulinDm1ProfileResult>>
{
    private readonly IInsulinDm1Repository _insulinRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpsertInsulinProfileCommandHandler(
        IInsulinDm1Repository insulinRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _insulinRepository = insulinRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<InsulinDm1ProfileResult>> Handle(
        UpsertInsulinProfileCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Authorize
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Load — single query resolves ownership + existence.
        //    "Not found" and "not yours" are collapsed into Forbidden to
        //    close the patient-ID enumeration oracle.
        var patient = await _patientRepository.GetOwnedPatientAsync(
            request.PatientId, userId, cancellationToken);
        if (patient is null)
            return AuthErrors.Forbidden;

        // 3. Guard — an inactive patient cannot record new data.
        if (!patient.IsActive)
            return RecordErrors.InactivePatient;

        var existing = await _insulinRepository.GetProfileByPatientIdAsync(request.PatientId);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (existing is null)
        {
            existing = new Domain.Models.InsulinDm1Profile
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                CreatedAt = now,
            };
        }

        existing.InsulinName = request.InsulinName;
        existing.Ric = request.Ric;
        existing.SensitivityFactor = request.SensitivityFactor;
        existing.TargetGlucose = request.TargetGlucose;
        existing.DoctorName = request.DoctorName;
        existing.DoctorPhone = request.DoctorPhone;
        existing.UpdatedAt = now;

        await _insulinRepository.UpsertProfileAsync(existing);

        return new InsulinDm1ProfileResult(
            existing.Id,
            existing.PatientId,
            existing.InsulinName,
            existing.Ric,
            existing.SensitivityFactor,
            existing.TargetGlucose,
            existing.DoctorName,
            existing.DoctorPhone,
            existing.CreatedAt,
            existing.UpdatedAt);
    }
}
