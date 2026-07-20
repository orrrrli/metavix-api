using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;

namespace Application.UseCases.Patient.Handlers;

internal sealed class GetPatientProfileQueryHandler
    : IRequestHandler<GetPatientProfileQuery, ErrorOr<PatientProfileResult>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetPatientProfileQueryHandler(
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<PatientProfileResult>> Handle(
        GetPatientProfileQuery request,
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
