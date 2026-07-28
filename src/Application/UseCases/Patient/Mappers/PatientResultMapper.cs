using Application.UseCases.Patient.Common;
using DomainPatient = Domain.Models.Patient;

namespace Application.UseCases.Patient.Mappers;

internal static class PatientResultMapper
{
    public static PatientResult ToResult(DomainPatient patient) => new(
        patient.Id,
        patient.FirstName,
        patient.LastName,
        patient.MedicalRecordNumber);
}
