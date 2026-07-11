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

    public static readonly Error NotAccepted = Error.Validation(
        code: "LinkRequest.NotAccepted",
        description: "Solo se puede revocar o desvincular una solicitud en estado Aceptada.");

    public static readonly Error MrnAlreadyAssigned = Error.Conflict(
        code: "LinkRequest.MrnAlreadyAssigned",
        description: "El número de historia clínica ya está asignado a otro paciente.");

    public static readonly Error MrnFormatInvalid = Error.Validation(
        code: "LinkRequest.MrnFormatInvalid",
        description: "El formato del MRN debe ser MRN-AAAA-NNNNNN.");

    public static readonly Error MrnAutoAssignFailed = Error.Failure(
        code: "LinkRequest.MrnAutoAssignFailed",
        description: "No se pudo asignar un número de historia clínica automáticamente. Intente nuevamente.");
}
