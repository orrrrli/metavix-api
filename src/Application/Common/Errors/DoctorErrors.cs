namespace Application.Common.Errors;

public static class DoctorErrors
{
    public static readonly Error DoctorNotFound = Error.NotFound(
        code: "Doctor.NotFound",
        description: "El doctor no fue encontrado.");

    public static readonly Error NotVerified = Error.Forbidden(
        code: "Doctor.NotVerified",
        description: "El doctor debe verificar su identidad antes de aceptar pacientes.");

    public static readonly Error InvalidLicense = Error.Validation(
        code: "Doctor.InvalidLicense",
        description: "El número de cédula profesional no fue encontrado en el registro de la SEP.");
}
