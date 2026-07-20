namespace Domain.Common.Errors;

using ErrorOr;

public static class GlucoseReadingErrors
{
    public static readonly Error InvalidValue = Error.Validation(
        code: "GlucoseReading.InvalidValue",
        description: "El valor de glucosa debe estar entre 20 y 800 mg/dL.");

    public static readonly Error TimeRequired = Error.Validation(
        code: "GlucoseReading.TimeRequired",
        description: "Esta categoría de lectura requiere una hora asociada.");
}
