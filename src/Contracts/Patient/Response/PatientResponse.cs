namespace Contracts.Patient.Response;

public record PatientResponse(
    Guid Id,
    string Name,
    string LastName,
    string MedicalRecordNumber);
