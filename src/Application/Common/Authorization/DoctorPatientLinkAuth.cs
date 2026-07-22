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
        var userIdResult = CurrentUserAccess.RequireUserId(currentUser);
        if (userIdResult.IsError)
            return userIdResult.FirstError;

        // Two round-trips: ownership first, then accepted link. Collapsing them
        // would couple the Doctors and PatientDoctorRequests repositories; the
        // extra call is cheap and only runs on doctor-scoped patient endpoints.
        var doctor = await doctorRepository.GetOwnedDoctorAsync(
            doctorId, userIdResult.Value, cancellationToken);
        if (doctor is null)
            return AuthErrors.Forbidden;


        var isLinked = await requestRepository.IsAcceptedLinkAsync(doctorId, patientId, cancellationToken);
        if (!isLinked)
            return AuthErrors.Forbidden;

        return null;
    }
}
