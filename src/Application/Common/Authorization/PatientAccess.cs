using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Models;

namespace Application.Common.Authorization;

/// <summary>
/// Shared guard for endpoints scoped to a patient acting on their own data:
/// the caller must be authenticated and must own the requested patient.
///
/// Collapses the six-line "authenticate + load-owned-patient" preamble that was
/// repeated across ~18 handlers. A single GetOwnedPatientAsync query resolves
/// ownership and existence together; both "not found" and "not yours" return
/// Forbidden to close the patient-id enumeration oracle.
/// </summary>
internal static class PatientAccess
{
    public static async Task<ErrorOr<Patient>> RequireOwnedPatientAsync(
        ICurrentUserService currentUser,
        IPatientRepository patientRepository,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserAccess.RequireUserId(currentUser, out var userId) is { } userIdError)
            return userIdError;

        var patient = await patientRepository.GetOwnedPatientAsync(patientId, userId, cancellationToken);
        return patient is null ? AuthErrors.Forbidden : patient;
    }
}
