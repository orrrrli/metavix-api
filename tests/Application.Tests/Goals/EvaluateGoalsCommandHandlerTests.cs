using Application.Common.Constants;
using Application.UseCases.Goals.Commands;
using Application.UseCases.Goals.Common;
using Application.UseCases.Goals.Handlers;

namespace Application.Tests.Goals;

public class EvaluateGoalsCommandHandlerTests
{
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly ILabResultRepository _labResultRepository = Substitute.For<ILabResultRepository>();
    private readonly IDailyRecordRepository _dailyRecordRepository = Substitute.For<IDailyRecordRepository>();
    private readonly IClinicalGoalRepository _clinicalGoalRepository = Substitute.For<IClinicalGoalRepository>();
    private readonly IGoalEvaluationRepository _goalEvaluationRepository = Substitute.For<IGoalEvaluationRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly EvaluateGoalsCommandHandler _handler;

    public EvaluateGoalsCommandHandlerTests()
    {
        _handler = new EvaluateGoalsCommandHandler(
            _patientRepository,
            _labResultRepository,
            _dailyRecordRepository,
            _clinicalGoalRepository,
            _goalEvaluationRepository,
            _currentUser,
            _timeProvider);
    }

    // T11: all parameters present → correct statuses per ADA thresholds
    [Fact]
    public async Task Handle_WhenAllParametersPresent_ReturnsCorrectStatusPerAdaThresholds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        // Weight 70 kg, height 170 cm → BMI = 70 / 1.7² = 24.2 → InRange (< 24.9 * 0.9 = 22.4)... actually 24.2 ≥ 22.41, so AtRisk
        // Let's use weight 60 kg → BMI = 60 / 1.7² = 20.76 → InRange (< 22.41)
        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = 110,   // InRange: < 130 * 0.9 = 117
            WeightKg = 60m,            // BMI = 20.76 → InRange
            GlucoseReadings =
            [
                new GlucoseReading { ReadingType = GlucoseReadingType.Fasting, ValueMgDl = 100 } // InRange: 80 ≤ 100 < 117
            ]
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 5.5m,   // InRange: < 7.0 * 0.9 = 6.3
            Ldl = 80m,       // InRange: < 100 * 0.9 = 90
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Should().AllSatisfy(i => i.Status.Should().Be(GoalStatus.InRange));
    }

    // T12: one parameter missing → Status=NoData for that item
    [Fact]
    public async Task Handle_WhenLabResultAbsent_ReturnsNoDataForHbA1cAndLdl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = 110,
            WeightKg = 60m,
            GlucoseReadings =
            [
                new GlucoseReading { ReadingType = GlucoseReadingType.Fasting, ValueMgDl = 100 }
            ]
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HbA1c)
            .Status.Should().Be(GoalStatus.NoData);

        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Ldl)
            .Status.Should().Be(GoalStatus.NoData);

        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.SystolicBp)
            .Status.Should().NotBe(GoalStatus.NoData);
    }

    // T13: ClinicalGoal override present → custom threshold applied
    [Fact]
    public async Task Handle_WhenClinicalGoalOverridePresent_UsesCustomThresholdInsteadOfAdaDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        // HbA1c = 7.5% → OutOfRange with ADA goal of 7.0
        // With custom goal of 9.0: AtRisk threshold = 9.0 * 0.9 = 8.1 → 7.5 < 8.1 → InRange
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 7.5m,
            Ldl = 80m,
        };

        var customGoal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = AdaGoalConstants.HbA1c,
            CustomValue = 9.0m,
        };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = 110,
            WeightKg = 60m,
            GlucoseReadings =
            [
                new GlucoseReading { ReadingType = GlucoseReadingType.Fasting, ValueMgDl = 100 }
            ]
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var hba1cItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HbA1c);
        hba1cItem.GoalUsed.Should().Be(9.0m);
        hba1cItem.Status.Should().Be(GoalStatus.InRange);
    }

    private void SetupAuth(Guid userId, Guid patientId, Patient patient)
    {
        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _timeProvider.SetUtcNow(DateTimeOffset.UtcNow);
        _goalEvaluationRepository.AddAsync(Arg.Any<GoalEvaluation>()).Returns(Task.CompletedTask);
    }
}
