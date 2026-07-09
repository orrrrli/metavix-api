using Application.Common.Constants;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
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
    private readonly TimeProvider _timeProvider;

    public EvaluateGoalsCommandHandler(
        IPatientRepository patientRepository,
        ILabResultRepository labResultRepository,
        IDailyRecordRepository dailyRecordRepository,
        IClinicalGoalRepository clinicalGoalRepository,
        IGoalEvaluationRepository goalEvaluationRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _patientRepository = patientRepository;
        _labResultRepository = labResultRepository;
        _dailyRecordRepository = dailyRecordRepository;
        _clinicalGoalRepository = clinicalGoalRepository;
        _goalEvaluationRepository = goalEvaluationRepository;
        _currentUser = currentUser;
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

        // Extract values from records
        decimal? sbp = allRecords.FirstOrDefault(r => r.SystolicPressure.HasValue)?.SystolicPressure;
        decimal? weight = allRecords.FirstOrDefault(r => r.WeightKg.HasValue)?.WeightKg;

        // T5: most recent fasting glucose across all records
        decimal? fastingGlucose = allRecords
            .SelectMany(r => r.GlucoseReadings)
            .Where(g => g.ReadingType == GlucoseReadingType.Fasting)
            .Select(g => (decimal?)g.ValueMgDl)
            .FirstOrDefault();

        // T6: BMI from weight + patient height
        decimal? bmi = null;
        if (weight.HasValue && patient.HeightCm is > 0)
        {
            var heightM = patient.HeightCm!.Value / 100m;
            bmi = weight.Value / (heightM * heightM);
        }

        // T8/T9: evaluate and build items
        var evaluationId = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Ldl resolves to ldl_primary (primary prevention) or ldl_secondary (established ASCVD,
        // stricter targets) based on Patient.HasAscvd, not patient category. Resolved here so
        // BuildItem stays parameter-agnostic — it only ever sees the final catalog id.
        var ldlParameterId = patient.HasAscvd ? AdaGoalConstants.LdlSecondary : AdaGoalConstants.LdlPrimary;

        var parameterValues = new (string ParameterId, decimal? Value)[]
        {
            (AdaGoalConstants.HbA1c,         latestLab?.Hba1c),
            (AdaGoalConstants.FastingGlucose, fastingGlucose),
            (AdaGoalConstants.SystolicBp,     sbp),
            (ldlParameterId,                  latestLab?.Ldl),
            (AdaGoalConstants.Bmi,            bmi),
            (AdaGoalConstants.Hdl,            latestLab?.Hdl),
        };

        var items = new List<GoalEvaluationItem>();
        foreach (var (parameterId, value) in parameterValues)
        {
            var item = BuildItem(evaluationId, parameterId, value, patient, customGoalMap);
            if (item is not null) items.Add(item);
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
                i.ParameterId, i.ValueUsed, i.GoalUsed, i.Status, i.Reason)).ToList());
    }

    // parameterId is always the final catalog id (e.g. "ldl_primary"/"ldl_secondary", already
    // resolved by the caller based on Patient.HasAscvd) — this method has no per-parameter
    // knowledge and never rewrites the id it was given.
    private static GoalEvaluationItem? BuildItem(
        Guid evaluationId,
        string parameterId,
        decimal? value,
        Domain.Models.Patient patient,
        Dictionary<string, ClinicalGoal> customGoalMap)
    {
        var category = AdaGoalConstants.ResolveCategory(patient.IsPregnant, patient.DiabetesType, parameterId);
        var spec = AdaGoalConstants.ResolveSpec(parameterId, category, patient.Gender);

        var hasCustom = customGoalMap.TryGetValue(parameterId, out var custom);

        // Decision 2A (matized): a genuine pregnancy-category spec (e.g. HbA1c or LDL targets in
        // gestation) takes precedence over any doctor-set custom goal. This also governs LDL: its
        // EmbarazadaDM catalog row makes IsPregnancySpecific true, so a custom ldl_primary/
        // ldl_secondary goal is intentionally ignored during pregnancy, same as HbA1c. Blood
        // pressure is the deliberate exception — it has no pregnancy-category row at all, so it
        // never reaches this branch and falls through to the specialist-custom-goal path below.
        if (spec is { IsPregnancySpecific: true })
            return BuildEvaluatedItem(evaluationId, parameterId, value, spec);

        if (spec is null)
        {
            // A non-pregnant patient simply has no applicable spec for this parameter → omit it.
            if (!patient.IsPregnant)
                return null;

            // Pregnant patient with no pregnancy-category spec and no Universal fallback
            // (currently only blood pressure): the specialist assigns it per patient via a
            // custom clinical goal. (If a future catalog parameter needs the "explicitly not
            // evaluated in pregnancy" case here too — i.e. AppliesInPregnancy=false on its base
            // category with no pregnancy row — reintroduce that check with test coverage; today
            // every non-Universal catalog row has AppliesInPregnancy=true, so it would be dead.)
            if (hasCustom)
                return BuildEvaluatedItem(evaluationId, parameterId, value, SpecFromCustom(parameterId, custom!));

            return BuildNoDataItem(evaluationId, parameterId, AdaGoalConstants.RequiresSpecialistEvaluationReason);
        }

        // Spec resolved but not evaluated during pregnancy (e.g. BMI, waist, total cholesterol —
        // Universal-category parameters marked AppliesInPregnancy=false).
        if (patient.IsPregnant && !spec.AppliesInPregnancy)
            return BuildNoDataItem(evaluationId, parameterId, AdaGoalConstants.NotEvaluatedInPregnancyReason);

        // Non-pregnancy-specific spec → apply the doctor's custom override when present.
        var effectiveSpec = hasCustom ? ApplyCustom(spec, custom!) : spec;
        return BuildEvaluatedItem(evaluationId, parameterId, value, effectiveSpec);
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
        Guid evaluationId, string parameterId, decimal? value, ParameterSpec spec)
    {
        // GoalUsed surfaces the InRange/AtRisk boundary the patient is compared against (custom or
        // spec default). Most specs are upper-bound-oriented (AtRiskHigh); low-only specs like HDL
        // and eGFR (higher is better) have no AtRiskHigh, so fall back to AtRiskLow, then to
        // whichever OutOfRange bound exists, in that order.
        var goal = spec.AtRiskHigh ?? spec.AtRiskLow ?? spec.OutOfRangeHigh ?? spec.OutOfRangeLow ?? 0m;

        return new GoalEvaluationItem
        {
            Id = Guid.NewGuid(),
            GoalEvaluationId = evaluationId,
            ParameterId = parameterId,
            ValueUsed = value,
            GoalUsed = goal,
            Status = spec.Classify(value),
        };
    }

    private static GoalEvaluationItem BuildNoDataItem(Guid evaluationId, string parameterId, string reason) =>
        new()
        {
            Id = Guid.NewGuid(),
            GoalEvaluationId = evaluationId,
            ParameterId = parameterId,
            ValueUsed = null,
            GoalUsed = 0m,
            Status = GoalStatus.NoData,
            Reason = reason,
        };
}
