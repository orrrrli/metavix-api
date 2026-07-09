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
        Guid patientId)
    {
        if (currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerDoctorId = await doctorRepository.GetDoctorIdByUserIdAsync(currentUser.UserId.Value);
        if (callerDoctorId != doctorId)
            return AuthErrors.Forbidden;

        var isLinked = await requestRepository.IsAcceptedLinkAsync(doctorId, patientId);
        if (!isLinked)
            return AuthErrors.Forbidden;

        return null;
    }
}
