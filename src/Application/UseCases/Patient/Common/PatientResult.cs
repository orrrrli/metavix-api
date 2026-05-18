namespace Application.UseCases.Patient.Common;

public record PatientResult(
    string Name,
    string LastName,
    string MedicalRecordNumber);