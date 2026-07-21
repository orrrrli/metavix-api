using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;

namespace Application.Common.Authorization;

// Shared guard for endpoints scoped to a doctor acting on one of their linked patients: the
// caller must be the doctor named in the route and must hold an accepted link with the patient.
internal static class DoctorPatientLinkAuth
{
    public static async Task<Error?> AuthorizeAsync(
        ICurrentUserService currentUser,
        IDoctorRepository doctorRepository,
        IPatientDoctorRequestRepository requestRepository,
        Guid doctorId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return AuthErrors.Forbidden;

        // This guard runs two queries, not one. The first resolves "doctor
        // exists" and "doctor is the caller" together in a single round-trip
        // (mirroring the GetOwnedDoctorAsync pattern from PR #255)...
        var doctor = await doctorRepository.GetOwnedDoctorAsync(
            doctorId, currentUser.UserId.Value, cancellationToken);
        if (doctor is null)
            return AuthErrors.Forbidden;

        // ...the second checks the accepted link. Kept as a separate query
        // because collapsing both into one join would couple the Doctors and
        // PatientDoctorRequests repositories; the extra round-trip is cheap and
        // only runs on the doctor-scoped patient endpoints.
        var isLinked = await requestRepository.IsAcceptedLinkAsync(doctorId, patientId, cancellationToken);
        if (!isLinked)
            return AuthErrors.Forbidden;

        return null;
    }
}
