using Domain.Enums;
using Domain.Models;

namespace Application.Common.Constants;

public static class AdaGoalConstants
{
    public const string HbA1c = "hba1c";
    public const string FastingGlucose = "fasting_glucose";
    public const string SystolicBp = "systolic_bp";
    public const string DiastolicBp = "diastolic_bp";
    public const string HeartRate = "heart_rate";
    public const string LdlPrimary = "ldl_primary";
    public const string LdlSecondary = "ldl_secondary";
    public const string Bmi = "bmi";
    public const string Hdl = "hdl";
    public const string TotalCholesterol = "total_cholesterol";
    public const string Triglycerides = "triglycerides";
    public const string Creatinine = "creatinine";
    public const string Egfr = "egfr";
    public const string Bun = "bun";
    public const string WaistCircumference = "waist_circumference";
    public const string Postprandial1h = "postprandial_1h";
    public const string Postprandial2h = "postprandial_2h";

    // Parameters EvaluateGoalsCommandHandler actually evaluates per evaluation pass. Smaller than
    // KnownParameterIds (which lists every catalog id a doctor may set a custom goal for). The
    // AdaGoalConstantsTests drift guard asserts the two stay in sync with the handler's
    // parameterValues array — adding a parameter to the catalog without wiring it through
    // evaluation (or vice versa) becomes a build failure.
    public static readonly IReadOnlySet<string> EvaluatedParameterIds =
        new HashSet<string>
        {
            HbA1c, FastingGlucose, SystolicBp, DiastolicBp, HeartRate, LdlPrimary, Bmi, Hdl,
            TotalCholesterol, Triglycerides, Creatinine, Egfr, Bun, WaistCircumference,
            Postprandial1h, Postprandial2h,
        };

    // NoDataReason values for GoalEvaluationItem.Reason. Shared as constants so a rename can't
    // silently desync production code from the tests asserting on it.
    public const string NotEvaluatedInPregnancyReason = "not-evaluated-in-pregnancy";
    public const string RequiresSpecialistEvaluationReason = "requires-specialist-evaluation";
    public const string NoRecentDataReason = "no-recent-data";

    // CKD stage labels (KDIGO 2024) emitted on GoalEvaluationItem.CkdStage when the
    // parameter is eGFR and a numeric value is present. Wire format is the label itself
    // (e.g. "G3a") — the frontend maps it to a localized description.
    public const string CkdStageG1 = "G1";
    public const string CkdStageG2 = "G2";
    public const string CkdStageG3a = "G3a";
    public const string CkdStageG3b = "G3b";
    public const string CkdStageG4 = "G4";
    public const string CkdStageG5 = "G5";

    public static readonly IReadOnlyList<ParameterSpec> Catalog = new List<ParameterSpec>
    {
        new("hba1c", PatientCategory.SinDiabetes, null, null, null, 5.7m, 6.5m, true, TimeSpan.FromDays(90)),
        new("hba1c", PatientCategory.ConDiabetes, null, null, null, 7.0m, 8.0m, true, TimeSpan.FromDays(90)),
        new("hba1c", PatientCategory.EmbarazadaDM, null, null, null, 6.0m, 7.0m, true, TimeSpan.FromDays(90)),

        new("fasting_glucose", PatientCategory.SinDiabetes, null, 60m, 70m, 100m, 126m, true, TimeSpan.FromDays(14)),
        new("fasting_glucose", PatientCategory.ConDiabetes, null, 70m, 80m, 131m, 180m, true, TimeSpan.FromDays(14)),
        new("fasting_glucose", PatientCategory.EmbarazadaDM, null, 60m, 70m, 96m, 110m, true, TimeSpan.FromDays(14)),

        new("postprandial_1h", PatientCategory.ConDiabetes, null, null, null, 180m, 250m, true, TimeSpan.FromDays(14)),
        new("postprandial_1h", PatientCategory.EmbarazadaDMG, null, 100m, 110m, 141m, 160m, true, TimeSpan.FromDays(14)),

        new("postprandial_2h", PatientCategory.ConDiabetes, null, null, null, 180m, 250m, true, TimeSpan.FromDays(14)),
        new("postprandial_2h", PatientCategory.EmbarazadaDMG, null, 90m, 100m, 121m, 140m, true, TimeSpan.FromDays(14)),

        // Blood pressure asymmetry is intentional and clinically grounded:
        //   - ConDiabetes: ADA 2026 Rec. 10.4 — target < 130/80; SBP < 120 if high CV/renal risk
        //     (BPROAD/ESPRIT evidence). The low-side band (OutOfRangeLow=90 SBP / 60 DBP) signals
        //     "reduce treatment" — relevant for diabetics on antihypertensives where over-treatment
        //     causes symptomatic hypotension.
        //   - SinDiabetes: ACC/AHA 2017 — normal < 120/80, elevated 120–129/< 80, HTN ≥ 130/80.
        //     No low-side band is defined for non-diabetics in the guideline, so an unusually low
        //     reading stays InRange rather than triggering an out-of-range signal.
        new("systolic_bp", PatientCategory.SinDiabetes, null, null, null, 120m, 130m, true, TimeSpan.FromDays(7)),
        new("systolic_bp", PatientCategory.ConDiabetes, null, 90m, null, 130m, 140m, true, TimeSpan.FromDays(7)),

        new("diastolic_bp", PatientCategory.SinDiabetes, null, null, null, 80m, 90m, true, TimeSpan.FromDays(7)),
        new("diastolic_bp", PatientCategory.ConDiabetes, null, 60m, null, 80m, 90m, true, TimeSpan.FromDays(7)),

        new("heart_rate", PatientCategory.Universal, null, 50m, 60m, 101m, 110m, true, TimeSpan.FromDays(7)),

        new("bmi", PatientCategory.Universal, null, 17m, 18.5m, 25m, 30m, false, TimeSpan.FromDays(30)),

        new("ldl_primary", PatientCategory.SinDiabetes, null, null, null, 130m, 160m, true, TimeSpan.FromDays(365)),
        new("ldl_primary", PatientCategory.ConDiabetes, null, null, null, 70m, 100m, true, TimeSpan.FromDays(365)),
        new("ldl_primary", PatientCategory.EmbarazadaDM, null, null, null, 70m, 100m, true, TimeSpan.FromDays(365)),

        new("ldl_secondary", PatientCategory.SinDiabetes, null, null, null, 100m, 130m, true, TimeSpan.FromDays(365)),
        new("ldl_secondary", PatientCategory.ConDiabetes, null, null, null, 55m, 70m, true, TimeSpan.FromDays(365)),
        new("ldl_secondary", PatientCategory.EmbarazadaDM, null, null, null, 55m, 70m, true, TimeSpan.FromDays(365)),

        new("hdl", PatientCategory.Universal, Gender.Female, 40m, 50m, null, null, true, TimeSpan.FromDays(365)),
        new("hdl", PatientCategory.Universal, Gender.Male, 35m, 40m, null, null, true, TimeSpan.FromDays(365)),

        new("total_cholesterol", PatientCategory.Universal, null, null, null, 200m, 240m, false, TimeSpan.FromDays(365)),

        new("triglycerides", PatientCategory.Universal, null, null, null, 150m, 500m, true, TimeSpan.FromDays(365)),

        new("creatinine", PatientCategory.Universal, Gender.Female, null, 0.5m, 1.2m, 1.4m, true, TimeSpan.FromDays(180)),
        new("creatinine", PatientCategory.Universal, Gender.Male, null, 0.7m, 1.3m, 1.5m, true, TimeSpan.FromDays(180)),

        new("egfr", PatientCategory.Universal, null, 30m, 60m, null, null, true, TimeSpan.FromDays(180)),

        new("bun", PatientCategory.Universal, null, 3m, 7m, 21m, 40m, true, TimeSpan.FromDays(180)),

        new("waist_circumference", PatientCategory.Universal, Gender.Female, null, null, 80m, 88m, false, TimeSpan.FromDays(30)),
        new("waist_circumference", PatientCategory.Universal, Gender.Male, null, null, 94m, 102m, false, TimeSpan.FromDays(30)),
    };

    // Every parameter a doctor may set a custom clinical goal for. LDL has no bare "ldl" entry:
    // EvaluateGoalsCommandHandler resolves it to ldl_primary or ldl_secondary (by Patient.HasAscvd)
    // before ever looking anything up, so a goal must be stored under one of those two ids.
    public static readonly IReadOnlySet<string> KnownParameterIds =
        Catalog.Select(s => s.ParameterId).ToHashSet();

    private static readonly HashSet<string> PostprandialParameterIds = new() { "postprandial_1h", "postprandial_2h" };

    // A pregnant patient with pre-existing Type1/Type2/LADA diabetes resolves postprandial glucose
    // to EmbarazadaDM (the `_` arm below), but the catalog only has postprandial rows for
    // ConDiabetes and EmbarazadaDMG — no EmbarazadaDM row. ResolveSpec then returns null, and
    // BuildItem's existing pregnancy fallback (the same path blood pressure uses in pregnancy)
    // takes over: a doctor-set custom goal if present, otherwise NoData
    // "requires-specialist-evaluation". See EvaluateGoalsCommandHandlerTests for coverage.
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

    public static ParameterSpec? ResolveSpec(string parameterId, PatientCategory category, Gender? gender)
    {
        if (category == PatientCategory.Universal)
        {
            var universal = Catalog.FirstOrDefault(s =>
                s.ParameterId == parameterId && s.Category == PatientCategory.Universal);
            if (universal is null) return null;
            if (universal.Gender is null) return universal;
            return universal.Gender == gender ? universal : null;
        }

        var categorySpec = Catalog.FirstOrDefault(s =>
            s.ParameterId == parameterId && s.Category == category);
        if (categorySpec is not null) return categorySpec;

        // Fall back to Universal if the parameter is not category-specific
        // (e.g., BMI, heart_rate, total_cholesterol, egfr, bun).
        var universalSpec = Catalog.FirstOrDefault(s =>
            s.ParameterId == parameterId && s.Category == PatientCategory.Universal);
        if (universalSpec is null) return null;
        if (universalSpec.Gender is null) return universalSpec;
        return universalSpec.Gender == gender ? universalSpec : null;
    }
}
