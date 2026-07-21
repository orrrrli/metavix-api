using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Doctor.Queries;
using Application.UseCases.Patient.Common;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetLinkedPatientProfileQueryHandler
    : IRequestHandler<GetLinkedPatientProfileQuery, ErrorOr<PatientProfileResult>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly ICurrentUserService _currentUser;

    public GetLinkedPatientProfileQueryHandler(
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IPatientDoctorRequestRepository requestRepository,
        ICurrentUserService currentUser)
    {
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _requestRepository = requestRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<PatientProfileResult>> Handle(
        GetLinkedPatientProfileQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Authorize — the caller must be the named doctor AND hold an accepted
        //    link with this specific patient. This closes the enumeration oracle:
        //    a doctor probing arbitrary patientIds gets Forbidden for every id
        //    they are not linked to, so they never reach the load step below.
        var authError = await DoctorPatientLinkAuth.AuthorizeAsync(
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId, cancellationToken);
        if (authError is not null)
            return authError.Value;

        // 2. Load — reaching here means an accepted link to this patient already
        //    exists, so the caller is authorized to know the patient by name.
        //    A null here is therefore an inconsistent state (a link pointing at a
        //    missing patient), not an enumeration probe — surface PatientNotFound
        //    honestly rather than masking it as Forbidden.
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
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
