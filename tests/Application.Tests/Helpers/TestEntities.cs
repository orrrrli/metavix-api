namespace Application.Tests.Helpers;

/// <summary>
/// Shared builders for the domain entities most tests need. Consolidates the
/// per-file BuildPatient/BuildDoctor copies. Every parameter has a
/// sensible default so call sites only specify what they assert on.
/// </summary>
public static class TestEntities
{
    public static Patient Patient(
        Guid id,
        bool isActive = true,
        string firstName = "Juan",
        string lastName = "Pérez",
        string email = "juan@mail.com",
        Guid? userId = null,
        Guid? primaryDoctorId = null,
        string? medicalRecordNumber = null) => new()
    {
        Id = id,
        UserId = userId ?? Guid.NewGuid(),
        IsActive = isActive,
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        PrimaryDoctorId = primaryDoctorId,
        MedicalRecordNumber = medicalRecordNumber,
    };

    public static PatientDoctorRequest LinkRequest(
        Guid requestId,
        Guid patientId,
        Guid doctorId,
        RequestStatus status = RequestStatus.Pending) => new()
    {
        Id = requestId,
        PatientId = patientId,
        DoctorId = doctorId,
        Status = status,
        CreatedAt = DateTime.UtcNow,
    };

    public static Doctor Doctor(
        Guid id,
        Guid? userId = null,
        string licenseNumber = "12345678",
        string speciality = "Endocrinología",
        bool isVerified = true,
        bool isActive = true) => new()
    {
        Id = id,
        UserId = userId ?? Guid.NewGuid(),
        FirstName = "Ana",
        PaternalLastName = "García",
        MaternalLastName = "López",
        LicenseNumber = licenseNumber,
        Speciality = speciality,
        Email = "ana@clinic.com",
        IsVerified = isVerified,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
    };
}
