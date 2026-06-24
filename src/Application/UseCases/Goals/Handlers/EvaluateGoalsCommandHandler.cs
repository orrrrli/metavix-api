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
        var customGoalMap = customGoals.ToDictionary(g => g.ParameterId, g => g.CustomValue);

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

        var items = new List<GoalEvaluationItem>
        {
            BuildItem(evaluationId, AdaGoalConstants.HbA1c,         latestLab?.Hba1c,  customGoalMap, AdaGoalConstants.HbA1cGoal),
            BuildItem(evaluationId, AdaGoalConstants.FastingGlucose, fastingGlucose,    customGoalMap, AdaGoalConstants.FastingGlucoseMax, AdaGoalConstants.FastingGlucoseMin),
            BuildItem(evaluationId, AdaGoalConstants.SystolicBp,     sbp,               customGoalMap, AdaGoalConstants.SystolicBpGoal),
            BuildItem(evaluationId, AdaGoalConstants.Ldl,            latestLab?.Ldl,    customGoalMap, AdaGoalConstants.LdlGoal),
            BuildItem(evaluationId, AdaGoalConstants.Bmi,            bmi,               customGoalMap, AdaGoalConstants.BmiMax, AdaGoalConstants.BmiMin),
        };

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
                i.ParameterId, i.ValueUsed, i.GoalUsed, i.Status)).ToList());
    }

    private static GoalEvaluationItem BuildItem(
        Guid evaluationId,
        string parameterId,
        decimal? value,
        Dictionary<string, decimal> customGoalMap,
        decimal adaGoal,
        decimal? lowerBound = null)
    {
        var goal = customGoalMap.GetValueOrDefault(parameterId, adaGoal);
        return new GoalEvaluationItem
        {
            Id = Guid.NewGuid(),
            GoalEvaluationId = evaluationId,
            ParameterId = parameterId,
            ValueUsed = value,
            GoalUsed = goal,
            Status = ClassifyStatus(value, goal, lowerBound),
        };
    }

    private static GoalStatus ClassifyStatus(decimal? value, decimal goal, decimal? lowerBound)
    {
        if (value is null)
            return GoalStatus.NoData;

        if (lowerBound.HasValue && value < lowerBound)
            return GoalStatus.OutOfRange;

        if (value >= goal)
            return GoalStatus.OutOfRange;

        if (value >= goal * AdaGoalConstants.AtRiskFactor)
            return GoalStatus.AtRisk;

        return GoalStatus.InRange;
    }
}
