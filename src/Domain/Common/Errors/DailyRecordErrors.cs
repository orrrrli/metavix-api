namespace Domain.Common.Errors;

using ErrorOr;

public static class DailyRecordErrors
{
    public static readonly Error IncompleteBloodPressure = Error.Validation(
        code: "Record.IncompleteBloodPressure",
        description: "SystolicPressure y DiastolicPressure deben proporcionarse juntas.");
}
