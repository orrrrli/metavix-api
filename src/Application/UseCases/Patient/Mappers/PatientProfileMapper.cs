using Application.UseCases.Patient.Common;
using DomainPatient = Domain.Models.Patient;

namespace Application.UseCases.Patient.Mappers;

internal static class PatientProfileMapper
{
    public static PatientProfileResult ToResult(DomainPatient patient) => new(
        patient.Id,
        patient.FirstName,
        patient.LastName,
        patient.Email,
        patient.Phone,
        patient.DateOfBirth,
        patient.HeightCm,
        patient.Gender?.ToString(),
        patient.IsPregnant,
        patient.DiabetesType.ToString(),
        patient.MedicalRecordNumber,
        patient.CreatedAt,
        patient.PregnancyStartDate,
        patient.PregnancyDueDate);
}
