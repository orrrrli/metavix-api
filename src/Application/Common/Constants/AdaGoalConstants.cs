namespace Application.Common.Constants;

public static class AdaGoalConstants
{
    public const string HbA1c = "hba1c";
    public const string FastingGlucose = "fasting_glucose";
    public const string SystolicBp = "systolic_bp";
    public const string Ldl = "ldl";
    public const string Bmi = "bmi";

    public const decimal HbA1cGoal = 7.0m;
    public const decimal FastingGlucoseMin = 80m;
    public const decimal FastingGlucoseMax = 130m;
    public const decimal SystolicBpGoal = 130m;
    public const decimal LdlGoal = 100m;
    public const decimal BmiMin = 18.5m;
    public const decimal BmiMax = 24.9m;

    public const decimal AtRiskFactor = 0.9m;
}
