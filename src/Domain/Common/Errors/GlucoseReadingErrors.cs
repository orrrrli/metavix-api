namespace Domain.Common.Errors;

using ErrorOr;

public static class GlucoseReadingErrors
{
    public static readonly Error InvalidValue = Error.Validation(
        code: "GlucoseReading.InvalidValue",
        description: "El valor de glucosa debe estar entre 1 y 600 mg/dL.");
}
