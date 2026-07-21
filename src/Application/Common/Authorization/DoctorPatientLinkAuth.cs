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

        // Single query resolves "doctor exists" and "doctor is the caller" together.
        // Mirrors the GetOwnedPatientAsync / GetOwnedDoctorAsync pattern used in
        // patient-loading and doctor-loading handlers (PR #255).
        var doctor = await doctorRepository.GetOwnedDoctorAsync(
            doctorId, currentUser.UserId.Value, cancellationToken);
        if (doctor is null)
            return AuthErrors.Forbidden;

        var isLinked = await requestRepository.IsAcceptedLinkAsync(doctorId, patientId, cancellationToken);
        if (!isLinked)
            return AuthErrors.Forbidden;

        return null;
    }
}
