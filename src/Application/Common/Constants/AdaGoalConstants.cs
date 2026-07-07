using Domain.Enums;
using Domain.Models;

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

    public static readonly IReadOnlyList<ParameterSpec> Catalog = new List<ParameterSpec>
    {
        new("hba1c", PatientCategory.SinDiabetes, null, null, null, 5.7m, 6.5m, true, TimeSpan.FromDays(90)),
        new("hba1c", PatientCategory.ConDiabetes, null, null, null, 7.0m, 8.0m, true, TimeSpan.FromDays(90)),
        new("hba1c", PatientCategory.EmbarazadaDM, null, null, null, 6.0m, 7.0m, true, TimeSpan.FromDays(90)),

        new("fasting_glucose", PatientCategory.SinDiabetes, null, 70m, 70m, 100m, 126m, true, TimeSpan.FromDays(14)),
        new("fasting_glucose", PatientCategory.ConDiabetes, null, 70m, 80m, 131m, 180m, true, TimeSpan.FromDays(14)),
        new("fasting_glucose", PatientCategory.EmbarazadaDM, null, 70m, 70m, 96m, 110m, true, TimeSpan.FromDays(14)),

        new("postprandial_1h", PatientCategory.ConDiabetes, null, null, null, 180m, 250m, true, TimeSpan.FromDays(14)),
        new("postprandial_1h", PatientCategory.EmbarazadaDMG, null, 110m, 110m, 141m, 160m, true, TimeSpan.FromDays(14)),

        new("postprandial_2h", PatientCategory.ConDiabetes, null, null, null, 180m, 250m, true, TimeSpan.FromDays(14)),
        new("postprandial_2h", PatientCategory.EmbarazadaDMG, null, 100m, 100m, 121m, 140m, true, TimeSpan.FromDays(14)),

        new("systolic_bp", PatientCategory.SinDiabetes, null, null, null, 120m, 130m, true, TimeSpan.FromDays(7)),
        new("systolic_bp", PatientCategory.ConDiabetes, null, null, null, 130m, 140m, true, TimeSpan.FromDays(7)),

        new("diastolic_bp", PatientCategory.SinDiabetes, null, null, null, 80m, 90m, true, TimeSpan.FromDays(7)),
        new("diastolic_bp", PatientCategory.ConDiabetes, null, null, null, 80m, 90m, true, TimeSpan.FromDays(7)),

        new("heart_rate", PatientCategory.Universal, null, 50m, 60m, 101m, 110m, true, TimeSpan.FromDays(7)),

        new("bmi", PatientCategory.Universal, null, 18.5m, 18.5m, 25m, 30m, false, TimeSpan.FromDays(30)),

        new("ldl_primary", PatientCategory.SinDiabetes, null, null, null, 130m, 160m, false, TimeSpan.FromDays(365)),
        new("ldl_primary", PatientCategory.ConDiabetes, null, null, null, 70m, 100m, false, TimeSpan.FromDays(365)),

        new("ldl_secondary", PatientCategory.SinDiabetes, null, null, null, 100m, 130m, false, TimeSpan.FromDays(365)),
        new("ldl_secondary", PatientCategory.ConDiabetes, null, null, null, 55m, 70m, false, TimeSpan.FromDays(365)),

        new("hdl", PatientCategory.Universal, Gender.Female, null, 50m, null, null, true, TimeSpan.FromDays(365)),
        new("hdl", PatientCategory.Universal, Gender.Male, null, 40m, null, null, true, TimeSpan.FromDays(365)),

        new("total_cholesterol", PatientCategory.Universal, null, null, null, 200m, 240m, false, TimeSpan.FromDays(365)),

        new("triglycerides", PatientCategory.Universal, null, null, null, 150m, 500m, true, TimeSpan.FromDays(365)),

        new("creatinine", PatientCategory.Universal, Gender.Female, null, 0.5m, 1.2m, 1.4m, true, TimeSpan.FromDays(180)),
        new("creatinine", PatientCategory.Universal, Gender.Male, null, 0.7m, 1.3m, 1.5m, true, TimeSpan.FromDays(180)),

        new("egfr", PatientCategory.Universal, null, 30m, 60m, null, null, true, TimeSpan.FromDays(180)),

        new("bun", PatientCategory.Universal, null, 7m, 7m, 21m, 40m, true, TimeSpan.FromDays(180)),

        new("waist_circumference", PatientCategory.Universal, Gender.Female, null, null, 80m, 88m, false, TimeSpan.FromDays(30)),
        new("waist_circumference", PatientCategory.Universal, Gender.Male, null, null, 94m, 102m, false, TimeSpan.FromDays(30)),
    };

    private static readonly HashSet<string> PostprandialParameterIds = new() { "postprandial_1h", "postprandial_2h" };

    public static PatientCategory ResolveCategory(bool isPregnant, DiabetesType diabetesType, string parameterId)
    {
        if (!isPregnant)
        {
            return diabetesType == DiabetesType.None ? PatientCategory.SinDiabetes : PatientCategory.ConDiabetes;
        }

        return diabetesType switch
        {
            DiabetesType.None       => PatientCategory.SinDiabetes,
            DiabetesType.Gestational => PostprandialParameterIds.Contains(parameterId)
                ? PatientCategory.EmbarazadaDMG
                : PatientCategory.EmbarazadaDM,
            _                       => PatientCategory.EmbarazadaDM
        };
    }
}
