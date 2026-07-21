using Application.Common.Authorization;
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
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var patient = access.Value;

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
