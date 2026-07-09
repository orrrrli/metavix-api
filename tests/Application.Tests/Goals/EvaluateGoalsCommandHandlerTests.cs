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

        // Weight 60 kg, height 170 cm → BMI = 60 / 1.7² = 20.76 → InRange (18.5 ≤ 20.76 < 25)
        // Thresholds now come from AdaGoalConstants.Catalog per patient category (SinDiabetes here).
        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = 110,   // SinDiabetes spec: < 120 → InRange
            WeightKg = 60m,            // BMI = 20.76 → InRange
            GlucoseReadings =
            [
                new GlucoseReading { ReadingType = GlucoseReadingType.Fasting, ValueMgDl = 90 } // SinDiabetes spec: 60 ≤ 90 < 100 → InRange
            ]
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 5.0m,   // SinDiabetes spec: < 5.7 → InRange
            Ldl = 80m,       // ldl_primary SinDiabetes spec: < 130 → InRange
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
        // 5 parameters + hdl. hdl is gender-specific and the test patient has no gender, so
        // the lookup returns null and hdl is omitted from the items list.
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

        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlPrimary)
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

        // HbA1c = 7.5% → OutOfRange with ADA goal of 7.0 (SinDiabetes AtRiskHigh=5.7, OutOfRangeHigh=6.5)
        // With custom upper bands moved to 9.0: 7.5 < 9.0 → InRange
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
            CustomAtRiskHigh = 9.0m,
            CustomOutOfRangeHigh = 9.0m,
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

    // Regression: a custom bound that only touches one side of the band must not leave a gap
    // against the catalog default on the other side (see ApplyCustom widening rule).
    [Fact]
    public async Task Handle_WhenCustomAtRiskHighExceedsCatalogOutOfRangeHigh_WidensOutOfRangeHighToMatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        // SinDiabetes HbA1c catalog: AtRiskHigh=5.7, OutOfRangeHigh=6.5.
        // Doctor sets only CustomAtRiskHigh=9.0, leaving OutOfRangeHigh at the catalog's 6.5 (< 9.0).
        // Without widening, 9.5 would fall through to AtRisk instead of OutOfRange.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 9.5m,
        };

        var customGoal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = AdaGoalConstants.HbA1c,
            CustomAtRiskHigh = 9.0m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var hba1cItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HbA1c);
        hba1cItem.Status.Should().Be(GoalStatus.OutOfRange);
    }

    // Regression: a custom goal stored under the canonical "ldl_primary" id (the id
    // CreateClinicalGoalCommandHandler actually accepts) must be found during evaluation, even
    // though the evaluation loop iterates the unresolved "ldl" alias internally.
    [Fact]
    public async Task Handle_WhenCustomGoalStoredUnderLdlPrimary_IsAppliedDuringEvaluation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        // ldl_primary SinDiabetes catalog: AtRiskHigh=130, OutOfRangeHigh=160. Custom relaxes both.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = 180m,
        };

        var customGoal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = AdaGoalConstants.LdlPrimary,
            CustomAtRiskHigh = 200m,
            CustomOutOfRangeHigh = 250m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var ldlItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlPrimary);
        ldlItem.GoalUsed.Should().Be(200m);
        ldlItem.Status.Should().Be(GoalStatus.InRange); // 180 < custom AtRiskHigh 200
    }

    private void SetupAuth(Guid userId, Guid patientId, Patient patient)
    {
        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _timeProvider.SetUtcNow(DateTimeOffset.UtcNow);
        _goalEvaluationRepository.AddAsync(Arg.Any<GoalEvaluation>()).Returns(Task.CompletedTask);
    }

    // T14: Female patient with low HDL → OutOfRange via gender-resolved spec
    [Fact]
    public async Task Handle_WhenFemalePatientHasLowHdl_ClassifiesAsOutOfRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hdl = 38m,   // Female HDL spec: OutOfRangeLow=40 → 38 < 40 → OutOfRange
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var hdlItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Hdl);
        hdlItem.Status.Should().Be(GoalStatus.OutOfRange);
        // Regression: HDL is a low-only spec (AtRiskHigh/OutOfRangeHigh are null), so GoalUsed
        // must fall back to AtRiskLow (50) instead of the old "?? 0m" phantom zero.
        hdlItem.GoalUsed.Should().Be(50m);
    }

    // Decision 2A: a genuine pregnancy-category spec wins over a doctor-set custom goal.
    [Fact]
    public async Task Handle_WhenPregnancySpecExists_CustomGoalIsIgnored()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
            IsPregnant = true,
            DiabetesType = DiabetesType.Type2,
        };

        // EmbarazadaDM HbA1c spec: AtRiskHigh=6.0, OutOfRangeHigh=7.0. Value 6.5 → AtRisk.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 6.5m,
        };

        // Doctor tries to relax the target to 9.0; the pregnancy spec must override it.
        var customGoal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = AdaGoalConstants.HbA1c,
            CustomAtRiskHigh = 9.0m,
            CustomOutOfRangeHigh = 9.0m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var hba1cItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HbA1c);
        hba1cItem.GoalUsed.Should().Be(6.0m);
        hba1cItem.Status.Should().Be(GoalStatus.AtRisk);
    }

    // Decision 2A also governs LDL: its EmbarazadaDM catalog row makes IsPregnancySpecific true,
    // so a custom goal is ignored during pregnancy, same as HbA1c above. HasAscvd decides which
    // id (ldl_primary vs ldl_secondary, both with an EmbarazadaDM row of the same shape) is
    // active; the two cases only differ in HasAscvd, the resolved id, the value, and the
    // catalog's AtRiskHigh for that id — parametrized rather than duplicated.
    [Theory]
    [InlineData(false, AdaGoalConstants.LdlPrimary, 85, 70)]
    [InlineData(true, AdaGoalConstants.LdlSecondary, 60, 55)]
    public async Task Handle_WhenPregnancySpecExists_LdlCustomGoalIsIgnored(
        bool hasAscvd, string expectedParameterId, decimal ldlValue, decimal expectedGoalUsed)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
            IsPregnant = true,
            DiabetesType = DiabetesType.Type2,
            HasAscvd = hasAscvd,
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = ldlValue,
        };

        // Doctor tries to relax the target; the pregnancy spec must override it.
        var customGoal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = expectedParameterId,
            CustomAtRiskHigh = 150m,
            CustomOutOfRangeHigh = 200m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var ldlItem = result.Value.Items.First(i => i.ParameterId == expectedParameterId);
        ldlItem.GoalUsed.Should().Be(expectedGoalUsed);
        ldlItem.Status.Should().Be(GoalStatus.AtRisk);
    }

    // When no pregnancy spec exists and no custom goal is set (blood pressure, which the
    // catalog deliberately omits for pregnancy categories), the parameter surfaces as NoData
    // with an explanatory reason instead of being silently dropped.
    [Fact]
    public async Task Handle_WhenSbpPregnancySpecMissing_AndIsPregnant_EmitsNoDataWithReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
            IsPregnant = true,
            DiabetesType = DiabetesType.Type2,
        };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = 128,   // systolic_bp has no EmbarazadaDM spec → requires specialist
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

        var sbpItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.SystolicBp);
        sbpItem.Status.Should().Be(GoalStatus.NoData);
        sbpItem.Reason.Should().Be(AdaGoalConstants.RequiresSpecialistEvaluationReason);
    }

    // A specialist-set custom goal fills the gap where the catalog has no pregnancy spec (e.g. SBP).
    [Fact]
    public async Task Handle_WhenPregnantAndNoSpecButCustomGoal_UsesCustomThresholds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
            IsPregnant = true,
            DiabetesType = DiabetesType.Type2,
        };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = 140,
        };

        // Specialist assigns SBP bands for this pregnant patient: AtRisk ≥135, OutOfRange ≥150.
        var customGoal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = AdaGoalConstants.SystolicBp,
            CustomAtRiskHigh = 135m,
            CustomOutOfRangeHigh = 150m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var sbpItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.SystolicBp);
        sbpItem.GoalUsed.Should().Be(135m);
        sbpItem.Status.Should().Be(GoalStatus.AtRisk);   // 135 ≤ 140 < 150
    }

    // BMI resolves via the Universal catalog fallback (AppliesInPregnancy=false), so a pregnant
    // patient hits the "spec resolved but not evaluated in pregnancy" branch, not the spec-is-null
    // one used by blood pressure. This path had no regression coverage before.
    [Fact]
    public async Task Handle_WhenPregnant_BmiEmitsNoDataWithNotEvaluatedReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
            IsPregnant = true,
            DiabetesType = DiabetesType.Type2,
        };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            WeightKg = 65m,
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

        var bmiItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Bmi);
        bmiItem.Status.Should().Be(GoalStatus.NoData);
        bmiItem.Reason.Should().Be(AdaGoalConstants.NotEvaluatedInPregnancyReason);
    }

    // Patient.HasAscvd routes Ldl to the stricter ldl_secondary spec instead of ldl_primary.
    // ldl_secondary was previously unreachable: nothing resolved it, so ASCVD patients were
    // always evaluated against the looser primary-prevention thresholds.
    [Fact]
    public async Task Handle_WhenPatientHasAscvd_UsesLdlSecondarySpec()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 170m,
            DiabetesType = DiabetesType.Type2,
            HasAscvd = true,
        };

        // ldl_secondary ConDiabetes spec: AtRiskHigh=55, OutOfRangeHigh=70. Value 80 → OutOfRange.
        // Under the (wrong) ldl_primary ConDiabetes spec (AtRiskHigh=70, OutOfRangeHigh=100) this
        // same value would only be AtRisk.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = 80m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var ldlItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlSecondary);
        ldlItem.GoalUsed.Should().Be(55m);
        ldlItem.Status.Should().Be(GoalStatus.OutOfRange);
    }

    // ResolveCategory maps a pregnant non-diabetic patient to SinDiabetes, not EmbarazadaDM —
    // EmbarazadaDM only exists for patients with an active diabetes diagnosis during pregnancy.
    // This predates the LDL catalog change and already governed HbA1c the same way; LDL simply
    // inherits it now that AppliesInPregnancy is true. Locking in the intended behavior: a
    // pregnant non-diabetic patient's LDL is evaluated against the plain SinDiabetes thresholds,
    // consistent with "cholesterol targets don't change with pregnancy."
    [Fact]
    public async Task Handle_WhenPregnantNonDiabetic_LdlUsesSinDiabetesSpec()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
            IsPregnant = true,
            DiabetesType = DiabetesType.None,
        };

        // ldl_primary SinDiabetes spec: AtRiskHigh=130, OutOfRangeHigh=160. Value 120 → InRange.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = 120m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var ldlItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlPrimary);
        ldlItem.GoalUsed.Should().Be(130m);
        ldlItem.Status.Should().Be(GoalStatus.InRange);
    }

    // Intentional consequence, not a bug: ParameterId is the override key, and HasAscvd changes
    // which id is active for LDL. A custom goal stored under "ldl_primary" does not carry over
    // once the patient's ASCVD status makes "ldl_secondary" the active id — the doctor must set a
    // new custom goal under the newly-active id. See clinical-goal.md for the documented rule.
    [Fact]
    public async Task Handle_WhenAscvdChangesActiveLdlId_PreviousLdlPrimaryCustomGoalNoLongerApplies()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 170m,
            DiabetesType = DiabetesType.Type2,
            HasAscvd = true,
        };

        // Doctor previously relaxed ldl_primary to 80/110, back when HasAscvd was false. Now that
        // HasAscvd is true, evaluation resolves to ldl_secondary instead, so this custom goal is
        // never looked up — the ldl_secondary ConDiabetes default (AtRiskHigh=55) applies instead.
        var customGoal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = AdaGoalConstants.LdlPrimary,
            CustomAtRiskHigh = 80m,
            CustomOutOfRangeHigh = 110m,
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = 60m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var ldlItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlSecondary);
        ldlItem.GoalUsed.Should().Be(55m); // ldl_secondary ConDiabetes default, not the ldl_primary custom
        ldlItem.Status.Should().Be(GoalStatus.AtRisk);
    }

    // Gestational diabetes wasn't covered by any existing LDL test: postprandial glucose splits
    // Gestational into EmbarazadaDMG, but LDL isn't in PostprandialParameterIds, so it resolves to
    // plain EmbarazadaDM — same ldl_primary/secondary spec as pre-existing Type1/Type2 diabetes.
    [Fact]
    public async Task Handle_WhenGestationalDiabetes_LdlUsesEmbarazadaDMSpec()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 165m,
            Gender = Gender.Female,
            IsPregnant = true,
            DiabetesType = DiabetesType.Gestational,
        };

        // ldl_primary EmbarazadaDM spec (same as ConDiabetes): AtRiskHigh=70, OutOfRangeHigh=100.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = 85m,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var ldlItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlPrimary);
        ldlItem.GoalUsed.Should().Be(70m);
        ldlItem.Status.Should().Be(GoalStatus.AtRisk);
    }
}
