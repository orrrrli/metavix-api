using Application.Common.Authorization;
using Application.Common.Errors;
using Application.UseCases.Patient.Mappers;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;

namespace Application.UseCases.Patient.Handlers;

internal sealed class GetMyPatientProfileQueryHandler
    : IRequestHandler<GetMyPatientProfileQuery, ErrorOr<PatientProfileResult>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyPatientProfileQueryHandler(
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _patientRepository = patientRepository;
        _currentUser       = currentUser;
    }

    public async Task<ErrorOr<PatientProfileResult>> Handle(
        GetMyPatientProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserAccess.RequireUserId(_currentUser, out var userId) is { } userIdError)
            return userIdError;

        var patient = await _patientRepository.GetByUserIdAsync(userId, cancellationToken);
        if (patient is null)
            return PatientErrors.PatientNotFound;

        return PatientProfileMapper.ToResult(patient);
    }
}
