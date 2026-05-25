namespace Application.UseCases.Patient.Common;

public sealed record PatientResumenResult(
    PatientPerfilResult Perfil,
    PatientMetricasResult Metricas);

public sealed record PatientPerfilResult(
    string Nombre,
    string TipoDiabetes,
    bool Embarazada,
    string Sexo);

public sealed record MetricaEntry(
    decimal? Valor,
    DateOnly? Fecha);

public sealed record PatientMetricasResult(
    MetricaEntry GlucosaAyuno,
    MetricaEntry PresionSistolica,
    MetricaEntry PresionDiastolica,
    MetricaEntry FrecuenciaCardiaca,
    MetricaEntry Peso,
    MetricaEntry EstaturasCm,
    MetricaEntry Imc,
    MetricaEntry Cintura,
    MetricaEntry Hba1c,
    MetricaEntry ColesterolTotal,
    MetricaEntry ColesterolLdl,
    MetricaEntry ColesterolHdl,
    MetricaEntry Trigliceridos,
    MetricaEntry Creatinina,
    MetricaEntry Bun);
