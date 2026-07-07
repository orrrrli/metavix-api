namespace Contracts.Patient.Request;

public record UpdatePatientProfileRequest(
    bool? IsPregnant,
    decimal? HeightCm,
    string? Phone,
    DateOnly? PregnancyStartDate,
    DateOnly? PregnancyDueDate);
