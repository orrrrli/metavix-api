using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Common;
using Application.UseCases.InsulinDm1.Queries;

namespace Application.UseCases.InsulinDm1.Handlers;

internal sealed class GetInsulinProfileQueryHandler
    : IRequestHandler<GetInsulinProfileQuery, ErrorOr<InsulinDm1ProfileResult>>
{
    private readonly IInsulinDm1Repository _insulinRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetInsulinProfileQueryHandler(
        IInsulinDm1Repository insulinRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _insulinRepository = insulinRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<InsulinDm1ProfileResult>> Handle(
        GetInsulinProfileQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var profile = await _insulinRepository.GetProfileByPatientIdAsync(request.PatientId);
        if (profile is null)
            return InsulinDm1Errors.ProfileNotFound;

        return new InsulinDm1ProfileResult(
            profile.Id,
            profile.PatientId,
            profile.InsulinName,
            profile.Ric,
            profile.SensitivityFactor,
            profile.TargetGlucose,
            profile.DoctorName,
            profile.DoctorPhone,
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
