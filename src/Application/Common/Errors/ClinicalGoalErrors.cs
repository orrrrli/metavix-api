namespace Application.Common.Errors;

public static class ClinicalGoalErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "ClinicalGoal.NotFound",
        description: "La meta clínica no fue encontrada.");

    public static readonly Error AlreadyExists = Error.Conflict(
        code: "ClinicalGoal.AlreadyExists",
        description: "Ya existe una meta clínica para este parámetro. Actualízala en su lugar.");

    public static readonly Error UnknownParameter = Error.Validation(
        code: "ClinicalGoal.UnknownParameter",
        description: "El parámetro indicado no existe en el catálogo de metas.");

    public static readonly Error NoThresholdsSet = Error.Validation(
        code: "ClinicalGoal.NoThresholdsSet",
        description: "Debe definirse al menos uno de los cuatro umbrales.");

    public static readonly Error IncoherentRange = Error.Validation(
        code: "ClinicalGoal.IncoherentRange",
        description: "Los umbrales no son coherentes: deben cumplir outOfRangeLow ≤ atRiskLow ≤ atRiskHigh ≤ outOfRangeHigh.");

    public static readonly Error PartialHighSide = Error.Validation(
        code: "ClinicalGoal.PartialHighSide",
        description: "Si se define atRiskHigh también debe definirse outOfRangeHigh.");

    public static readonly Error PartialLowSide = Error.Validation(
        code: "ClinicalGoal.PartialLowSide",
        description: "Si se define atRiskLow también debe definirse outOfRangeLow.");
}
