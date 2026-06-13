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
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var patientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (patientId is null)
            return PatientErrors.PatientNotFound;

        var patient = await _patientRepository.GetByIdAsync(patientId.Value);
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
            patient.CreatedAt);
    }
}
