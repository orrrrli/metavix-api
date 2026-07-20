namespace Application.Common.Errors;

public static class RecordErrors
{
    public static readonly Error RecordNotFound = Error.NotFound(
        code: "Record.NotFound",
        description: "El registro no fue encontrado.");

    public static readonly Error RecordsNotFound = Error.NotFound(
        code: "Record.NotFound",
        description: "No se encontraron registros.");

    public static readonly Error InactivePatient = Error.Validation(
        code: "Record.InactivePatient",
        description: "El paciente está inactivo y no puede registrar DailyRecords.");
}
