namespace Application.Common.Errors;

public static class PatientErrors
{
    public static Error PatientsNotFound =>
        Error.NotFound("PatientErrors.patient_not_found", "Pacientes no encontrados");
    
    public static Error PatientNotFound => 
        Error.NotFound("PatientErrors.patient_not_found", "Paciente no encontrado");
}