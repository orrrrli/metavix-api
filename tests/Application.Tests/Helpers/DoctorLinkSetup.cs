namespace Application.Tests.Helpers;

/// <summary>
/// Wires up the NSubstitute mocks for the doctor→patient link authorization path
/// (DoctorPatientLinkAuth): the caller is the named doctor and holds an accepted
/// link with the patient. Both flags default to true.
/// <paramref name="doctorOwned"/> = false simulates a caller who isn't the named
/// doctor (GetOwnedDoctorAsync returns null); <paramref name="linked"/> = false
/// simulates no accepted link. (false, false) is valid too: ownership fails first,
/// so the link check is never reached.
/// </summary>
public static class DoctorLinkSetup
{
    public static void Authorize(
        ICurrentUserService currentUser,
        IDoctorRepository doctorRepository,
        IPatientDoctorRequestRepository requestRepository,
        Guid userId,
        Guid doctorId,
        Guid patientId,
        bool linked = true,
        bool doctorOwned = true)
    {
        currentUser.UserId.Returns(userId);
        doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>())
            .Returns(doctorOwned ? TestEntities.Doctor(doctorId, userId) : null);
        requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>())
            .Returns(linked);
    }
}
