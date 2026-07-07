namespace Domain.Enums;

/// <summary>
/// Patient subpopulation for <see cref="Models.ParameterSpec"/> resolution.
/// </summary>
public enum PatientCategory
{
    /// <summary>Patient without diabetes (IsPregnant=false, DiabetesType=None).</summary>
    SinDiabetes,

    /// <summary>Patient with diabetes (IsPregnant=false, DiabetesType in Type1/Type2/LADA/Gestational).</summary>
    ConDiabetes,

    /// <summary>Pregnant patient with pre-existing diabetes (IsPregnant=true, DiabetesType in Type1/Type2/LADA).</summary>
    EmbarazadaDM,

    /// <summary>Pregnant patient with gestational diabetes — only used for postprandial_1h and postprandial_2h; other parameters use EmbarazadaDM.</summary>
    EmbarazadaDMG,

    /// <summary>Thresholds identical across all categories (HeartRate, BMI, TotalCholesterol, Triglycerides, eGFR, BUN, gender-based params).</summary>
    Universal
}
