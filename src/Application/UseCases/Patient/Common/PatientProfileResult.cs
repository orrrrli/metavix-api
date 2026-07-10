namespace Application.UseCases.Patient.Common;

public sealed record PatientProfileResult(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateOnly DateOfBirth,
    decimal? HeightCm,
    string? Gender,
    bool IsPregnant,
    string DiabetesType,
    string MedicalRecordNumber,
    DateTime CreatedAt,
    DateOnly? PregnancyStartDate,
    DateOnly? PregnancyDueDate);
