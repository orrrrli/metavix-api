using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Common;
using Application.UseCases.InsulinDm1.Mappers;

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
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var patient = access.Value;

        // 2. Guard — an inactive patient cannot record new data.
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

        return InsulinDm1ProfileMapper.ToResult(existing);
    }
}
