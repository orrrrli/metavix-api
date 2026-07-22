namespace Application.Tests.Helpers;

/// <summary>
/// Shared id-tuple generators for tests that only need fresh, unrelated Guids to
/// wire up mocks. Replaces per-file "var userId = Guid.NewGuid(); ..." boilerplate.
/// </summary>
public static class TestIds
{
    public static (Guid UserId, Guid DoctorId, Guid PatientId) DoctorLink() =>
        (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    public static (Guid UserId, Guid DoctorId, Guid PatientId, Guid RequestId) LinkRequest() =>
        (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
}
