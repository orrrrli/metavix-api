namespace Application.Common.Errors;

public static class LinkRequestErrors
{
    public static readonly Error RequestNotFound = Error.NotFound(
        code: "LinkRequest.NotFound",
        description: "La solicitud de vinculación no fue encontrada.");

    public static readonly Error AlreadyPending = Error.Conflict(
        code: "LinkRequest.AlreadyPending",
        description: "Ya existe una solicitud de vinculación pendiente con este doctor.");

    public static readonly Error AlreadyLinked = Error.Conflict(
        code: "LinkRequest.AlreadyLinked",
        description: "El paciente ya está vinculado con un doctor.");

    public static readonly Error NotPending = Error.Validation(
        code: "LinkRequest.NotPending",
        description: "Solo se pueden aceptar solicitudes en estado Pendiente.");
}
