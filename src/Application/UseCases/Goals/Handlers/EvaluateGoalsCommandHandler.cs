using Application.Common.Constants;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Services;
using Application.UseCases.Goals.Commands;
using Application.UseCases.Goals.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Goals.Handlers;

internal sealed class EvaluateGoalsCommandHandler
    : IRequestHandler<EvaluateGoalsCommand, ErrorOr<EvaluateGoalsResult>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILabResultRepository _labResultRepository;
    private readonly IDailyRecordRepository _dailyRecordRepository;
    private readonly IClinicalGoalRepository _clinicalGoalRepository;
    private readonly IGoalEvaluationRepository _goalEvaluationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IEgfrCalculator _egfrCalculator;
    private readonly TimeProvider _timeProvider;

    public EvaluateGoalsCommandHandler(
        IPatientRepository patientRepository,
        ILabResultRepository labResultRepository,
        IDailyRecordRepository dailyRecordRepository,
        IClinicalGoalRepository clinicalGoalRepository,
        IGoalEvaluationRepository goalEvaluationRepository,
        ICurrentUserService currentUser,
        IEgfrCalculator egfrCalculator,
        TimeProvider timeProvider)
    {
        _patientRepository = patientRepository;
        _labResultRepository = labResultRepository;
        _dailyRecordRepository = dailyRecordRepository;
        _clinicalGoalRepository = clinicalGoalRepository;
        _goalEvaluationRepository = goalEvaluationRepository;
        _currentUser = currentUser;
        _egfrCalculator = egfrCalculator;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<EvaluateGoalsResult>> Handle(
        EvaluateGoalsCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
            return PatientErrors.PatientsNotFound;

        // T3: latest lab result for HbA1c and LDL
        var latestLab = await _labResultRepository.GetLatestByPatientIdAsync(request.PatientId);

        // T4/T5/T6: all records include GlucoseReadings via repository Include
        var allRecords = await _dailyRecordRepository.GetAllByPatientIdAsync(request.PatientId);

        // T7: custom goal overrides
        var customGoals = await _clinicalGoalRepository.GetByPatientIdAsync(request.PatientId);
        var customGoalMap = customGoals.ToDictionary(g => g.ParameterId, g => g);

        // T8/T9: evaluate and build items
        var evaluationId = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        // Extract values with their measurement date. Records arrive newest-first, so the first
        // record carrying a given vital is the most recent reading for it (a vital may be absent on
        // some days). The date lets BuildItem enforce each spec's NoDataWindow.
        (decimal? Value, DateOnly? MeasuredOn) LatestVital(Func<Domain.Models.DailyRecord, decimal?> selector)
        {
            var record = allRecords.FirstOrDefault(r => selector(r).HasValue);
            return record is null ? (null, null) : (selector(record), record.RecordDate);
        }

        var (sbp, sbpDate) = LatestVital(r => r.SystolicPressure);
        var (dbp, dbpDate) = LatestVital(r => r.DiastolicPressure);
        var (heartRate, heartRateDate) = LatestVital(r => r.HeartRate);
        var (waist, waistDate) = LatestVital(r => r.WaistCm);
        var (weight, weightDate) = LatestVital(r => r.WeightKg);

        // Most recent reading explicitly marked 1h/2h post-meal, regardless of which meal.
        // The marker (GlucoseReading.PostprandialWindow) is set at capture time — see #235.
        (decimal? Value, DateOnly? MeasuredOn) LatestPostprandial(PostprandialWindow window)
        {
            var record = allRecords.FirstOrDefault(r => r.GlucoseReadings.Any(g => g.PostprandialWindow == window));
            if (record is null) return (null, null);
            return (record.GlucoseReadings.First(g => g.PostprandialWindow == window).ValueMgDl, record.RecordDate);
        }

        var (postprandial1h, postprandial1hDate) = LatestPostprandial(PostprandialWindow.OneHour);
        var (postprandial2h, postprandial2hDate) = LatestPostprandial(PostprandialWindow.TwoHour);

        // Lab-derived values all share the sample date of the latest lab result.
        DateOnly? labDate = latestLab?.SampleDate;

        // T5: most recent fasting glucose across all records, with its record date.
        var fastingRecord = allRecords
            .FirstOrDefault(r => r.GlucoseReadings.Any(g => g.ReadingType == GlucoseReadingType.Fasting));
        decimal? fastingGlucose = fastingRecord?.GlucoseReadings
            .First(g => g.ReadingType == GlucoseReadingType.Fasting).ValueMgDl;
        DateOnly? fastingDate = fastingRecord?.RecordDate;

        // T6: BMI from weight + patient height; freshness tracks the weight reading.
        decimal? bmi = null;
        if (weight.HasValue && patient.HeightCm is > 0)
        {
            var heightM = patient.HeightCm!.Value / 100m;
            bmi = weight.Value / (heightM * heightM);
        }

        // Ldl resolves to ldl_primary (primary prevention) or ldl_secondary (established ASCVD,
        // stricter targets) based on Patient.HasAscvd, not patient category. Resolved here so
        // BuildItem stays parameter-agnostic — it only ever sees the final catalog id.
        var ldlParameterId = patient.HasAscvd ? AdaGoalConstants.LdlSecondary : AdaGoalConstants.LdlPrimary;

        // eGFR is derived, not measured: CKD-EPI 2021 from the latest creatinine + age + sex.
        // Returns null (→ NoData) when creatinine or gender is missing; freshness tracks the lab.
        var ageYears = AgeInYears(patient.DateOfBirth, today);
        decimal? egfr = _egfrCalculator.Calculate(latestLab?.Creatinine, ageYears, patient.Gender);

        // Source of truth lives in AdaGoalConstants.EvaluatedParameterIds — the AdaGoalConstantsTests
        // drift guard fails the build if this list and the catalog diverge, so don't edit it without
        // updating the set (and adding a row to the catalog if it's a new parameter).
        var parameterValues = new (string ParameterId, decimal? Value, DateOnly? MeasuredOn)[]
        {
            (AdaGoalConstants.HbA1c,            latestLab?.Hba1c,            labDate),
            (AdaGoalConstants.FastingGlucose,   fastingGlucose,             fastingDate),
            (AdaGoalConstants.SystolicBp,       sbp,                        sbpDate),
            (AdaGoalConstants.DiastolicBp,      dbp,                        dbpDate),
            (AdaGoalConstants.HeartRate,        heartRate,                  heartRateDate),
            (ldlParameterId,                    latestLab?.Ldl,             labDate),
            (AdaGoalConstants.Bmi,              bmi,                        weightDate),
            (AdaGoalConstants.Hdl,              latestLab?.Hdl,             labDate),
            (AdaGoalConstants.TotalCholesterol, latestLab?.TotalCholesterol, labDate),
            (AdaGoalConstants.Triglycerides,    latestLab?.Triglycerides,   labDate),
            (AdaGoalConstants.Creatinine,       latestLab?.Creatinine,      labDate),
            (AdaGoalConstants.Egfr,             egfr,                       labDate),
            (AdaGoalConstants.Bun,              latestLab?.Bun,             labDate),
            (AdaGoalConstants.WaistCircumference, waist,                    waistDate),
            (AdaGoalConstants.Postprandial1h,   postprandial1h,             postprandial1hDate),
            (AdaGoalConstants.Postprandial2h,   postprandial2h,             postprandial2hDate),
        };

        var items = new List<GoalEvaluationItem>();
        foreach (var (parameterId, value, measuredOn) in parameterValues)
        {
            var (item, isCustom) = BuildItem(evaluationId, parameterId, value, measuredOn, today, patient, customGoalMap);
            if (item is not null)
            {
                // KDIGO stage is eGFR-specific clinical context, not a generic per-item property.
                // Set it here (caller knows the parameterId) rather than threading it through
                // BuildItem, which stays parameter-agnostic.
                if (parameterId == AdaGoalConstants.Egfr)
                    item.CkdStage = CkdStageClassifier.Classify(item.ValueUsed);
                item.IsCustomGoal = isCustom;
                items.Add(item);
            }
        }

        var evaluation = new GoalEvaluation
        {
            Id = evaluationId,
            PatientId = request.PatientId,
            TriggeredBy = request.TriggeredBy,
            EvaluatedAt = now,
            Items = items,
        };

        await _goalEvaluationRepository.AddAsync(evaluation);

        return new EvaluateGoalsResult(
            evaluationId,
            now,
            items.Select(i => new GoalEvaluationItemResult(
                i.ParameterId, i.ValueUsed, i.ThresholdUsed, i.Status, i.Reason, i.CkdStage, i.IsCustomGoal)).ToList());
    }

    // Whole years elapsed from birth to the evaluation date, subtracting one if the birthday
    // hasn't occurred yet this year.
    private static int AgeInYears(DateOnly dateOfBirth, DateOnly onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (dateOfBirth > onDate.AddYears(-age))
            age--;
        return age;
    }

    // parameterId is always the final catalog id (e.g. "ldl_primary"/"ldl_secondary", already
    // resolved by the caller based on Patient.HasAscvd) — this method has no per-parameter
    // knowledge and never rewrites the id it was given.
    //
    // Returns (item, isCustom): isCustom is true when the item's effective thresholds came
    // from a doctor-set ClinicalGoal (merged via ApplyCustom or built from custom alone in
    // the pregnancy-no-spec branch). False when only the ADA catalog spec was used. Null
    // item means the parameter is omitted entirely (e.g. non-pregnant patient with no
    // applicable spec, which excludes both catalog and custom code paths).
    private static (GoalEvaluationItem? Item, bool IsCustom) BuildItem(
        Guid evaluationId,
        string parameterId,
        decimal? value,
        DateOnly? measuredOn,
        DateOnly today,
        Domain.Models.Patient patient,
        Dictionary<string, ClinicalGoal> customGoalMap)
    {
        var category = AdaGoalConstants.ResolveCategory(patient.IsPregnant, patient.DiabetesType, parameterId);
        var spec = AdaGoalConstants.ResolveSpec(parameterId, category, patient.Gender);

        var hasCustom = customGoalMap.TryGetValue(parameterId, out var custom);

        // NoDataWindow: a value measured longer ago than the spec's freshness window is too stale
        // to evaluate, so it surfaces as NoData "no-recent-data" rather than being classified
        // against a boundary. Only the catalog spec carries a window (custom-only specs, e.g. BP in
        // pregnancy, have none), and only present, dated values can be stale.
        if (value is not null
            && spec?.NoDataWindow is { } window
            && measuredOn is { } measured
            && today.DayNumber - measured.DayNumber > window.TotalDays)
        {
            return (BuildNoDataItem(evaluationId, parameterId, AdaGoalConstants.NoRecentDataReason, hasCustom), hasCustom);
        }

        // Decision 2A (matized): a genuine pregnancy-category spec (e.g. HbA1c or LDL targets in
        // gestation) takes precedence over any doctor-set custom goal. This also governs LDL: its
        // EmbarazadaDM catalog row makes IsPregnancySpecific true, so a custom ldl_primary/
        // ldl_secondary goal is intentionally ignored during pregnancy, same as HbA1c. Blood
        // pressure is the deliberate exception — it has no pregnancy-category row at all, so it
        // never reaches this branch and falls through to the specialist-custom-goal path below.
        if (spec is { IsPregnancySpecific: true })
            return (BuildEvaluatedItem(evaluationId, parameterId, value, spec, isCustom: false), false);

        if (spec is null)
        {
            // A non-pregnant patient simply has no applicable spec for this parameter → omit it.
            if (!patient.IsPregnant)
                return (null, false);

            // Pregnant patient with no pregnancy-category spec and no Universal fallback
            // (currently only blood pressure): the specialist assigns it per patient via a
            // custom clinical goal. (If a future catalog parameter needs the "explicitly not
            // evaluated in pregnancy" case here too — i.e. AppliesInPregnancy=false on its base
            // category with no pregnancy row — reintroduce that check with test coverage; today
            // every non-Universal catalog row has AppliesInPregnancy=true, so it would be dead.)
            if (hasCustom)
            {
                // No reading yet — surface the same "specialist must evaluate" reason the
                // no-custom branch emits, instead of letting BuildEvaluatedItem emit a bare
                // NoData item that loses the explanation.
                if (value is null)
                    return (BuildNoDataItem(evaluationId, parameterId, AdaGoalConstants.RequiresSpecialistEvaluationReason, isCustom: true), true);

                return (BuildEvaluatedItem(evaluationId, parameterId, value, SpecFromCustom(parameterId, custom!), isCustom: true), true);
            }

            return (BuildNoDataItem(evaluationId, parameterId, AdaGoalConstants.RequiresSpecialistEvaluationReason, isCustom: false), false);
        }

        // Spec resolved but not evaluated during pregnancy (e.g. BMI, waist, total cholesterol —
        // Universal-category parameters marked AppliesInPregnancy=false).
        if (patient.IsPregnant && !spec.AppliesInPregnancy)
            return (BuildNoDataItem(evaluationId, parameterId, AdaGoalConstants.NotEvaluatedInPregnancyReason, isCustom: hasCustom), hasCustom);

        // Non-pregnancy-specific spec → apply the doctor's custom override when present.
        var effectiveSpec = hasCustom ? ApplyCustom(spec, custom!) : spec;

        // No reading for a parameter that does have a spec (e.g. HDL Female with no lab result).
        // BuildEvaluatedItem used to emit Status=NoData with ThresholdUsed set to a fallback band —
        // misleading because ThresholdUsed is the comparison threshold, and a NoData item has no
        // threshold because it was not evaluated. Route through BuildNoDataItem so ThresholdUsed
        // stays null.
        if (value is null)
            return (BuildNoDataItem(evaluationId, parameterId, AdaGoalConstants.RequiresSpecialistEvaluationReason, isCustom: hasCustom), hasCustom);

        return (BuildEvaluatedItem(evaluationId, parameterId, value, effectiveSpec, isCustom: hasCustom), hasCustom);
    }

    // A null custom threshold keeps the catalog default; a set one overrides that band.
    // ThresholdRange.MergeOnto widens bands outward as needed so the merged spec never violates
    // outOfRangeLow <= atRiskLow <= atRiskHigh <= outOfRangeHigh.
    private static ParameterSpec ApplyCustom(ParameterSpec spec, ClinicalGoal custom)
    {
        var merged = new ThresholdRange(
                custom.CustomOutOfRangeLow, custom.CustomAtRiskLow, custom.CustomAtRiskHigh, custom.CustomOutOfRangeHigh)
            .MergeOnto(new ThresholdRange(spec.OutOfRangeLow, spec.AtRiskLow, spec.AtRiskHigh, spec.OutOfRangeHigh));

        return spec with
        {
            OutOfRangeLow = merged.OutOfRangeLow,
            AtRiskLow = merged.AtRiskLow,
            AtRiskHigh = merged.AtRiskHigh,
            OutOfRangeHigh = merged.OutOfRangeHigh,
        };
    }

    // Builds a spec entirely from the specialist's custom bands when the catalog has no row
    // (e.g. blood pressure targets for a pregnant patient).
    private static ParameterSpec SpecFromCustom(string parameterId, ClinicalGoal custom) =>
        new(parameterId, PatientCategory.Universal, null,
            custom.CustomOutOfRangeLow, custom.CustomAtRiskLow,
            custom.CustomAtRiskHigh, custom.CustomOutOfRangeHigh,
            AppliesInPregnancy: true, NoDataWindow: null);

    private static GoalEvaluationItem BuildEvaluatedItem(
        Guid evaluationId, string parameterId, decimal? value, ParameterSpec spec, bool isCustom)
    {
        // ThresholdUsed surfaces the InRange/AtRisk boundary the patient is compared against after
        // ApplyCustom has merged the custom goal onto the catalog spec. Most specs are
        // upper-bound-oriented (AtRiskHigh); low-only specs like HDL and eGFR (higher is better)
        // have no AtRiskHigh, so fall back to AtRiskLow, then to whichever OutOfRange bound
        // exists, in that order. A spec with no bounds at all yields null — surfacing that as 0m
        // would be a phantom threshold for an unmeasurable parameter.
        var threshold = spec.AtRiskHigh ?? spec.AtRiskLow ?? spec.OutOfRangeHigh ?? spec.OutOfRangeLow;

        return new GoalEvaluationItem
        {
            Id = Guid.NewGuid(),
            GoalEvaluationId = evaluationId,
            ParameterId = parameterId,
            ValueUsed = value,
            ThresholdUsed = threshold,
            Status = spec.Classify(value),
            IsCustomGoal = isCustom,
        };
    }

    private static GoalEvaluationItem BuildNoDataItem(Guid evaluationId, string parameterId, string reason, bool isCustom) =>
        new()
        {
            Id = Guid.NewGuid(),
            GoalEvaluationId = evaluationId,
            ParameterId = parameterId,
            ValueUsed = null,
            ThresholdUsed = null,
            Status = GoalStatus.NoData,
            Reason = reason,
            IsCustomGoal = isCustom,
        };
}
