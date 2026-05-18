namespace Contracts.Patient.Response;

public record PatientResponse(
    string Name,
    string LastName,
    string MedicalRecordNumber);
