using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;

namespace Application.UseCases.Patient.Handlers;

internal sealed class PatientByIdQueryHandler(
    IPatientRepository patientRepository,
    IDoctorRepository doctorRepository,
    IPatientDoctorRequestRepository requestRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<PatientByIdQuery, ErrorOr<PatientResult>>
{
    public async Task<ErrorOr<PatientResult>> Handle(
        PatientByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Authorize — the route doctorId must be the caller's doctor AND
        //    an accepted link must exist with this patient. This closes the
        //    enumeration oracle: a doctor probing arbitrary patientIds gets
        //    Forbidden for every id they are not linked to, so they never
        //    reach the load step below.
        var authError = await DoctorPatientLinkAuth.AuthorizeAsync(
            currentUser, doctorRepository, requestRepository,
            request.DoctorId, request.PatientId, cancellationToken);
        if (authError is not null)
            return authError.Value;

        // 2. Load — reaching here means an accepted link to this patient
        //    already exists, so the caller is authorized to know the patient
        //    by name. A null here is an inconsistent state (a link pointing
        //    at a missing patient), not an enumeration probe — surface
        //    PatientNotFound honestly rather than masking it as Forbidden.
        // TODO: see IPatientRepository.GetPatientByPatientId — cancellationToken
        // dropped on this last round-trip; propagate once the method takes a CT.
        PatientResult? result = await patientRepository.GetPatientByPatientId(request.PatientId);
        if (result is null)
            return PatientErrors.PatientNotFound;

        return result;
    }
}
