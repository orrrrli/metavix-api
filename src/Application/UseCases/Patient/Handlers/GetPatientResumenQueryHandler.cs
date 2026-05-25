using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using Domain.Enums;

namespace Application.UseCases.Patient.Handlers;

internal sealed class GetPatientResumenQueryHandler
    : IRequestHandler<GetPatientResumenQuery, ErrorOr<PatientResumenResult>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly ILabResultRepository _labResultRepository;
    private readonly ICurrentUserService _currentUser;

    public GetPatientResumenQueryHandler(
        IPatientRepository patientRepository,
        IDailyRecordRepository dailyRecordRepository,
        ILabResultRepository labResultRepository,
        ICurrentUserService currentUser)
    {
        _patientRepository = patientRepository;
        _dailyRecordRepository = dailyRecordRepository;
        _labResultRepository = labResultRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<PatientResumenResult>> Handle(
        GetPatientResumenQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
            return RecordErrors.RecordNotFound;

        var latestRecord = await _dailyRecordRepository.GetLatestByPatientIdAsync(request.PatientId);
        var latestLab = await _labResultRepository.GetLatestByPatientIdAsync(request.PatientId);

        var fastingReading = latestRecord?.GlucoseReadings
            .Where(g => g.ReadingType == GlucoseReadingType.Fasting)
            .OrderByDescending(g => g.Time)
            .FirstOrDefault();

        decimal? imc = null;
        if (patient.HeightCm.HasValue && latestRecord?.WeightKg.HasValue == true)
        {
            decimal heightM = patient.HeightCm.Value / 100m;
            imc = Math.Round(latestRecord.WeightKg.Value / (heightM * heightM), 1);
        }

        var perfil = new PatientPerfilResult(
            Nombre: $"{patient.FirstName} {patient.LastName}",
            TipoDiabetes: MapDiabetesType(patient.DiabetesType),
            Embarazada: patient.IsPregnant,
            Sexo: patient.Gender == Gender.Male ? "M" : "F");

        var metricas = new PatientMetricasResult(
            GlucosaAyuno: new MetricaEntry(
                fastingReading is not null ? (decimal?)fastingReading.ValueMgDl : null,
                fastingReading is not null ? latestRecord!.RecordDate : null),
            PresionSistolica: new MetricaEntry(
                latestRecord?.SystolicPressure,
                latestRecord?.SystolicPressure.HasValue == true ? latestRecord.RecordDate : null),
            PresionDiastolica: new MetricaEntry(
                latestRecord?.DiastolicPressure,
                latestRecord?.DiastolicPressure.HasValue == true ? latestRecord.RecordDate : null),
            FrecuenciaCardiaca: new MetricaEntry(
                latestRecord?.HeartRate,
                latestRecord?.HeartRate.HasValue == true ? latestRecord.RecordDate : null),
            Peso: new MetricaEntry(
                latestRecord?.WeightKg,
                latestRecord?.WeightKg.HasValue == true ? latestRecord.RecordDate : null),
            EstaturasCm: new MetricaEntry(
                patient.HeightCm,
                null),
            Imc: new MetricaEntry(
                imc,
                imc.HasValue ? latestRecord!.RecordDate : null),
            Cintura: new MetricaEntry(
                latestRecord?.WaistCm,
                latestRecord?.WaistCm.HasValue == true ? latestRecord.RecordDate : null),
            Hba1c: new MetricaEntry(
                latestLab?.Hba1c,
                latestLab?.Hba1c.HasValue == true ? latestLab.SampleDate : null),
            ColesterolTotal: new MetricaEntry(
                latestLab?.TotalCholesterol,
                latestLab?.TotalCholesterol.HasValue == true ? latestLab.SampleDate : null),
            ColesterolLdl: new MetricaEntry(
                latestLab?.Ldl,
                latestLab?.Ldl.HasValue == true ? latestLab.SampleDate : null),
            ColesterolHdl: new MetricaEntry(
                latestLab?.Hdl,
                latestLab?.Hdl.HasValue == true ? latestLab.SampleDate : null),
            Trigliceridos: new MetricaEntry(
                latestLab?.Triglycerides,
                latestLab?.Triglycerides.HasValue == true ? latestLab.SampleDate : null),
            Creatinina: new MetricaEntry(
                latestLab?.Creatinine,
                latestLab?.Creatinine.HasValue == true ? latestLab.SampleDate : null),
            Bun: new MetricaEntry(
                latestLab?.Bun,
                latestLab?.Bun.HasValue == true ? latestLab.SampleDate : null));

        return new PatientResumenResult(perfil, metricas);
    }

    private static string MapDiabetesType(DiabetesType type) => type switch
    {
        DiabetesType.Type1       => "tipo_1",
        DiabetesType.Type2       => "tipo_2",
        DiabetesType.Prediabetes => "prediabetes",
        _                        => "none"
    };
}
