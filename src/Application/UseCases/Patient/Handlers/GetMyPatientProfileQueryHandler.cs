using Application.Common.Errors;
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
        // 1. Authorize
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Load — caller is fetching their own patient profile, so a single
        //    by-userId lookup is the right granularity (no patientId is supplied
        //    in this query). A null result means the authenticated user simply
        //    has no patient profile yet — that is a missing resource, not a
        //    permissions failure, so surface PatientNotFound (not Forbidden).
        var patient = await _patientRepository.GetByUserIdAsync(userId, cancellationToken);
        if (patient is null)
            return PatientErrors.PatientNotFound;

        return new PatientProfileResult(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.Email,
            patient.Phone,
            patient.DateOfBirth,
            patient.HeightCm,
            patient.Gender?.ToString(),
            patient.IsPregnant,
            patient.DiabetesType.ToString(),
            patient.MedicalRecordNumber,
            patient.CreatedAt,
            patient.PregnancyStartDate,
            patient.PregnancyDueDate);
    }
}
