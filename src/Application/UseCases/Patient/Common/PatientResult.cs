namespace Application.UseCases.Patient.Common;

public record PatientResult(
    Guid Id,
    string Name,
    string LastName,
    string? MedicalRecordNumber);