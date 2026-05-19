namespace Application.Common.Errors;

public static class DoctorErrors
{
    public static readonly Error DoctorNotFound = Error.NotFound(
        code: "Doctor.NotFound",
        description: "El doctor no fue encontrado.");
}
