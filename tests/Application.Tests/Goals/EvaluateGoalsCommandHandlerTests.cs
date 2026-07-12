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
    private readonly IEgfrCalculator _egfrCalculator = Substitute.For<IEgfrCalculator>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly EvaluateGoalsCommandHandler _handler;

    public EvaluateGoalsCommandHandlerTests()
    {
        // Keep the handler tests focused on wiring/classification, not the CKD-EPI math (covered
        // in EgfrCalculatorTests): return an in-range eGFR whenever creatinine is present, null
        // otherwise, mirroring the real calculator's null-on-missing-input contract.
        _egfrCalculator
            .Calculate(Arg.Any<decimal?>(), Arg.Any<int>(), Arg.Any<Gender?>())
            .Returns(ci => ci.ArgAt<decimal?>(0) is null ? (decimal?)null : 95m);

        _handler = new EvaluateGoalsCommandHandler(
            _patientRepository,
            _labResultRepository,
            _dailyRecordRepository,
            _clinicalGoalRepository,
            _goalEvaluationRepository,
            _currentUser,
            _egfrCalculator,
            _timeProvider);
    }

    // T11: all parameters present → correct statuses per ADA thresholds
    [Fact]
    public async Task Handle_WhenAllParametersPresent_ReturnsCorrectStatusPerAdaThresholds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        // Female so the gender-specific specs (hdl, creatinine, waist_circumference) resolve.
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m, Gender = Gender.Female };

        // Weight 60 kg, height 170 cm → BMI = 60 / 1.7² = 20.76 → InRange (18.5 ≤ 20.76 < 25).
        // Every value below sits inside the SinDiabetes/Universal/Female InRange band for its spec.
        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = 110,   // SinDiabetes: < 120 → InRange
            DiastolicPressure = 70,   // SinDiabetes: < 80 → InRange
            HeartRate = 70,           // Universal: 60 ≤ 70 < 101 → InRange
            WeightKg = 60m,           // BMI = 20.76 → InRange
            WaistCm = 70,             // Female: < 80 → InRange
            GlucoseReadings =
            [
                new GlucoseReading { ReadingType = GlucoseReadingType.Fasting, ValueMgDl = 90 } // SinDiabetes: 60 ≤ 90 < 100 → InRange
            ]
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 5.0m,             // SinDiabetes: < 5.7 → InRange
            Ldl = 80m,                // ldl_primary SinDiabetes: < 130 → InRange
            Hdl = 60m,                // Female: ≥ 50 → InRange
            TotalCholesterol = 180m,  // Universal: < 200 → InRange
            Triglycerides = 100m,     // Universal: < 150 → InRange
            Creatinine = 1.0m,        // Female: 0.5 ≤ 1.0 < 1.2 → InRange
            Bun = 15m,                // Universal: 7 ≤ 15 < 21 → InRange
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
        // All evaluated parameters are present and in range, except postprandial_1h/2h: they have
        // no SinDiabetes catalog row, so BuildItem omits them entirely for this non-pregnant,
        // non-diabetic patient (see AdaGoalConstants.ResolveSpec's non-pregnant "omit" branch).
        result.Value.Items.Should().HaveCount(AdaGoalConstants.EvaluatedParameterIds.Count - 2);
        result.Value.Items.Should().AllSatisfy(i => i.Status.Should().Be(GoalStatus.InRange));
    }

    // Locks in the wiring of the newly-evaluated parameters: each is read from the right
    // record/lab field and classified against its catalog spec, not silently dropped.
    [Fact]
    public async Task Handle_WhenNewlyWiredParametersOutOfRange_ClassifiesEachPerCatalog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m, Gender = Gender.Female };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            DiastolicPressure = 95,   // SinDiabetes: ≥ 90 → OutOfRange
            HeartRate = 120,          // Universal: ≥ 110 → OutOfRange
            WaistCm = 85,             // Female: 80 ≤ 85 < 88 → AtRisk
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            TotalCholesterol = 260m,  // Universal: ≥ 240 → OutOfRange
            Triglycerides = 200m,     // Universal: 150 ≤ 200 < 500 → AtRisk
            Creatinine = 1.5m,        // Female: ≥ 1.4 → OutOfRange
            Bun = 45m,                // Universal: ≥ 40 → OutOfRange
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

        GoalStatus StatusOf(string id) => result.Value.Items.First(i => i.ParameterId == id).Status;
        StatusOf(AdaGoalConstants.DiastolicBp).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.HeartRate).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.WaistCircumference).Should().Be(GoalStatus.AtRisk);
        StatusOf(AdaGoalConstants.TotalCholesterol).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Triglycerides).Should().Be(GoalStatus.AtRisk);
        StatusOf(AdaGoalConstants.Creatinine).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Bun).Should().Be(GoalStatus.OutOfRange);
    }

    // eGFR is derived from creatinine via IEgfrCalculator: the handler must feed the latest
    // creatinine to the calculator and classify its output against the egfr catalog spec
    // (≥60 InRange, 30–60 AtRisk, <30 OutOfRange).
    [Fact]
    public async Task Handle_WhenCreatininePresent_EvaluatesEgfrFromCalculatorOutput()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 170m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1956, 1, 1),
        };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Creatinine = 1.8m,
        };

        // Override the default fake: this creatinine yields a G3b eGFR → AtRisk band (30–60).
        _egfrCalculator
            .Calculate(1.8m, Arg.Any<int>(), Gender.Male)
            .Returns(42m);

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var egfrItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Egfr);
        egfrItem.ValueUsed.Should().Be(42m);
        egfrItem.Status.Should().Be(GoalStatus.AtRisk);
    }

    // The eGFR item carries a KDIGO 2024 CKD stage alongside the band status. The stage
    // is derived from the calculator's output (not from the band), so this test pins the
    // classifier contract at the handler boundary.
    [Fact]
    public async Task Handle_WhenEgfrIs45_SetsCkdStageToG3a()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 170m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1956, 1, 1),
        };

        _egfrCalculator
            .Calculate(Arg.Any<decimal?>(), Arg.Any<int>(), Gender.Male)
            .Returns(45m);

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Creatinine = 1.8m,
        });
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        var egfrItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Egfr);
        egfrItem.ValueUsed.Should().Be(45m);
        egfrItem.CkdStage.Should().Be(AdaGoalConstants.CkdStageG3a);
    }

    // No creatinine → calculator returns null → eGFR is NoData → CkdStage is null.
    // The classifier leaves the column empty so the UI hides the educational section.
    [Fact]
    public async Task Handle_WhenEgfrIsNull_DoesNotSetCkdStage()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 170m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1956, 1, 1),
        };

        // The constructor's default stub returns null when no creatinine is passed in
        // (no lab below), so we just don't pass one.
        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        var egfrItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Egfr);
        egfrItem.ValueUsed.Should().BeNull();
        egfrItem.CkdStage.Should().BeNull();
    }

    // NoDataWindow: a present value measured longer ago than its spec's freshness window is too
    // stale to classify and surfaces as NoData "no-recent-data" instead of a band status. BP's
    // window is 7 days; HbA1c's is 90, so the same-dated HbA1c stays evaluable — proving the window
    // is applied per parameter, not globally.
    [Fact]
    public async Task Handle_WhenValueOlderThanNoDataWindow_EmitsNoDataWithNoRecentDataReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        // EvaluationNow is 2026-06-22; this reading is 30 days old → outside BP's 7-day window.
        var staleDate = new DateOnly(2026, 5, 23);
        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = staleDate,
            SystolicPressure = 110,   // in-range value, but stale
        };

        // HbA1c dated the same day is still inside its 90-day window → evaluated normally.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = staleDate,
            Hba1c = 5.0m,
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

        var sbpItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.SystolicBp);
        sbpItem.Status.Should().Be(GoalStatus.NoData);
        sbpItem.Reason.Should().Be("no-recent-data");

        var hba1cItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HbA1c);
        hba1cItem.Status.Should().Be(GoalStatus.InRange);
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
        hba1cItem.ThresholdUsed.Should().Be(9.0m);
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

    // Regression: a custom goal stored under "ldl_primary" (the id CreateClinicalGoalCommandHandler
    // accepts) must be found during evaluation. The handler resolves the active LDL id from
    // Patient.HasAscvd before iterating the parameterValues table, so for a non-ASCVD patient
    // BuildItem is called with "ldl_primary" directly and the lookup hits the custom goal.
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
        ldlItem.ThresholdUsed.Should().Be(200m);
        ldlItem.Status.Should().Be(GoalStatus.InRange); // 180 < custom AtRiskHigh 200
    }

    // Fixtures date their records/labs on 2026-06-21; evaluating one day later keeps every value
    // comfortably inside its NoDataWindow (shortest is BP/heart-rate at 7 days), so the freshness
    // guard doesn't turn otherwise-valid fixtures into NoData. A fixed clock also keeps the
    // date-sensitive assertions deterministic.
    private static readonly DateTimeOffset EvaluationNow = new(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);

    private void SetupAuth(Guid userId, Guid patientId, Patient patient)
    {
        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _timeProvider.SetUtcNow(EvaluationNow);
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
        // Regression: HDL is a low-only spec (AtRiskHigh/OutOfRangeHigh are null), so ThresholdUsed
        // must fall back to AtRiskLow (50) instead of the old "?? 0m" phantom zero.
        hdlItem.ThresholdUsed.Should().Be(50m);
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
        hba1cItem.ThresholdUsed.Should().Be(6.0m);
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
        bool hasAscvd, string expectedParameterId, decimal ldlValue, decimal expectedThresholdUsed)
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
        ldlItem.ThresholdUsed.Should().Be(expectedThresholdUsed);
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
        sbpItem.ThresholdUsed.Should().Be(135m);
        sbpItem.Status.Should().Be(GoalStatus.AtRisk);   // 135 ≤ 140 < 150
    }

    // Specialist custom SBP exists for a pregnant patient but no daily record has been logged yet.
    // BuildItem previously called BuildEvaluatedItem with SpecFromCustom, which emits a NoData
    // item (value=null) without a Reason — silently dropping the "specialist must evaluate"
    // explanation. The fix routes this exact shape through BuildNoDataItem so the reason survives.
    [Fact]
    public async Task Handle_WhenPregnantAndSbpCustomGoalButNoReading_EmitsNoDataWithSpecialistReason()
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

        // Specialist has set SBP bands for this patient, but no daily record exists yet — no SBP value.
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
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);   // no readings
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var sbpItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.SystolicBp);
        sbpItem.Status.Should().Be(GoalStatus.NoData);
        sbpItem.Reason.Should().Be(AdaGoalConstants.RequiresSpecialistEvaluationReason);
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
        ldlItem.ThresholdUsed.Should().Be(55m);
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
        ldlItem.ThresholdUsed.Should().Be(130m);
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
        ldlItem.ThresholdUsed.Should().Be(55m); // ldl_secondary ConDiabetes default, not the ldl_primary custom
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
        ldlItem.ThresholdUsed.Should().Be(70m);
        ldlItem.Status.Should().Be(GoalStatus.AtRisk);
    }

    // Finding 3: a parameter with a resolvable spec but no reading (e.g. HDL Female with no lab
    // result) used to emit Status=NoData with ThresholdUsed set to a fallback band from the spec — a
    // phantom threshold for an unevaluated parameter. ThresholdUsed is the comparison threshold,
    // and a NoData item was not evaluated, so ThresholdUsed must be null. The handler now routes
    // value=null through BuildNoDataItem, which sets ThresholdUsed=null alongside Status=NoData
    // and the explanatory reason.
    [Fact]
    public async Task Handle_WhenSpecResolvedButNoReading_ThresholdUsedIsNullWithSpecialistReason()
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
            IsPregnant = false,
            DiabetesType = DiabetesType.Type2,
        };

        // No lab result → HDL value is null, but HDL Universal Female spec exists.
        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        var hdlItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Hdl);
        hdlItem.Status.Should().Be(GoalStatus.NoData);
        hdlItem.ThresholdUsed.Should().BeNull("a NoData item has no threshold because it was not evaluated");
        hdlItem.ValueUsed.Should().BeNull();
        hdlItem.Reason.Should().Be(AdaGoalConstants.RequiresSpecialistEvaluationReason);
    }

    // #235: ConDiabetes shares one threshold (180/250) across postprandial_1h and postprandial_2h —
    // the window only changes which catalog row EmbarazadaDMG resolves to (see Theory below).
    [Theory]
    [InlineData(AdaGoalConstants.Postprandial1h, PostprandialWindow.OneHour, 150, GoalStatus.InRange)]
    [InlineData(AdaGoalConstants.Postprandial1h, PostprandialWindow.OneHour, 200, GoalStatus.AtRisk)]
    [InlineData(AdaGoalConstants.Postprandial1h, PostprandialWindow.OneHour, 260, GoalStatus.OutOfRange)]
    [InlineData(AdaGoalConstants.Postprandial2h, PostprandialWindow.TwoHour, 150, GoalStatus.InRange)]
    [InlineData(AdaGoalConstants.Postprandial2h, PostprandialWindow.TwoHour, 200, GoalStatus.AtRisk)]
    [InlineData(AdaGoalConstants.Postprandial2h, PostprandialWindow.TwoHour, 260, GoalStatus.OutOfRange)]
    public async Task Handle_ConDiabetes_ClassifiesPostprandialByWindowMarker(
        string parameterId, PostprandialWindow window, int value, GoalStatus expectedStatus)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            DiabetesType = DiabetesType.Type2,
        };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            GlucoseReadings =
            [
                new GlucoseReading
                {
                    ReadingType = GlucoseReadingType.PostBreakfast,
                    ValueMgDl = value,
                    PostprandialWindow = window,
                }
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

        var item = result.Value.Items.First(i => i.ParameterId == parameterId);
        item.ValueUsed.Should().Be(value);
        item.Status.Should().Be(expectedStatus);
    }

    // #235: gestational diabetes (EmbarazadaDMG) has stricter, window-specific thresholds —
    // 1h: 141/160, 2h: 121/140 — distinct from ConDiabetes's flat 180/250.
    [Theory]
    [InlineData(AdaGoalConstants.Postprandial1h, PostprandialWindow.OneHour, 130, GoalStatus.InRange)]
    [InlineData(AdaGoalConstants.Postprandial1h, PostprandialWindow.OneHour, 150, GoalStatus.AtRisk)]
    [InlineData(AdaGoalConstants.Postprandial1h, PostprandialWindow.OneHour, 165, GoalStatus.OutOfRange)]
    [InlineData(AdaGoalConstants.Postprandial2h, PostprandialWindow.TwoHour, 110, GoalStatus.InRange)]
    [InlineData(AdaGoalConstants.Postprandial2h, PostprandialWindow.TwoHour, 130, GoalStatus.AtRisk)]
    [InlineData(AdaGoalConstants.Postprandial2h, PostprandialWindow.TwoHour, 145, GoalStatus.OutOfRange)]
    public async Task Handle_EmbarazadaDMG_ClassifiesPostprandialByWindowMarker(
        string parameterId, PostprandialWindow window, int value, GoalStatus expectedStatus)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            IsPregnant = true,
            DiabetesType = DiabetesType.Gestational,
        };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            GlucoseReadings =
            [
                new GlucoseReading
                {
                    ReadingType = GlucoseReadingType.PostBreakfast,
                    ValueMgDl = value,
                    PostprandialWindow = window,
                }
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

        var item = result.Value.Items.First(i => i.ParameterId == parameterId);
        item.ValueUsed.Should().Be(value);
        item.Status.Should().Be(expectedStatus);
    }

    // #235 KNOWN GAP resolution: a pregnant patient with pre-existing diabetes (not gestational)
    // resolves postprandial to EmbarazadaDM, which has no postprandial catalog row. This must fall
    // back to the same "specialist must evaluate" path blood pressure already uses in pregnancy,
    // not silently disappear.
    [Fact]
    public async Task Handle_EmbarazadaDM_PostprandialWithNoCatalogRow_EmitsNoDataWithSpecialistReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            IsPregnant = true,
            DiabetesType = DiabetesType.Type2,   // pre-existing, not gestational
        };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            GlucoseReadings =
            [
                new GlucoseReading
                {
                    ReadingType = GlucoseReadingType.PostBreakfast,
                    ValueMgDl = 145,
                    PostprandialWindow = PostprandialWindow.OneHour,
                }
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

        var item = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Postprandial1h);
        item.Status.Should().Be(GoalStatus.NoData);
        item.Reason.Should().Be(AdaGoalConstants.RequiresSpecialistEvaluationReason);
    }

    // ====================================================================================
    // #186: 16 RFs × 3 value scenarios grid.
    //
    // The grid exists to catch catalog threshold drift: if a single number in
    // AdaGoalConstants.Catalog is edited, the matching theory fails with a name that points
    // straight at the parameter and band. Behavior tests above (custom goals, ASCVD rerouting,
    // pregnancy, NoDataWindow, postprandial window marker) stay where they are — the grid is
    // only the threshold-drift canary.
    //
    // Each theory exercises the *default category* for the RF (SinDiabetes for hba1c /
    // fasting / BP / LDL; Universal for HR / BMI / HDL / TC / TG / creatinine / eGFR / BUN /
    // waist; ConDiabetes for postprandial 1h / 2h because they have no SinDiabetes row).
    // Boundary choice: ParameterSpec.Classify treats the high edge as inclusive
    // (>= OutOfRangeHigh → OutOfRange), so each value is picked strictly between two bands
    // to keep the assertion unambiguous.
    // ====================================================================================

    // RF-001 · hba1c · SinDiabetes: InRange < 5.7, AtRisk 5.7–6.4, OutOfRange ≥ 6.5.
    [Theory]
    [InlineData(5.0, GoalStatus.InRange)]
    [InlineData(6.0, GoalStatus.AtRisk)]
    [InlineData(7.0, GoalStatus.OutOfRange)]
    public async Task Handle_HbA1c_SinDiabetes_ClassifiesByCatalogBand(decimal hba1c, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = hba1c,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HbA1c)
            .Status.Should().Be(expected);
    }

    // RF-002 · fasting_glucose · SinDiabetes: InRange 70–99, AtRisk 100–125, OutOfRange ≥ 126.
    [Theory]
    [InlineData(90, GoalStatus.InRange)]
    [InlineData(110, GoalStatus.AtRisk)]
    [InlineData(130, GoalStatus.OutOfRange)]
    public async Task Handle_FastingGlucose_SinDiabetes_ClassifiesByCatalogBand(int value, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            GlucoseReadings = [new GlucoseReading { ReadingType = GlucoseReadingType.Fasting, ValueMgDl = value }],
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.FastingGlucose)
            .Status.Should().Be(expected);
    }

    // RF-003 · postprandial_1h · ConDiabetes: InRange < 180, AtRisk 180–250, OutOfRange ≥ 250.
    // The existing postprandial window-marker theory covers EmbarazadaDMG / ConDiabetes × 1h/2h
    // in depth; this row is the band-drift canary for ConDiabetes 1h only.
    [Theory]
    [InlineData(150, GoalStatus.InRange)]
    [InlineData(200, GoalStatus.AtRisk)]
    [InlineData(260, GoalStatus.OutOfRange)]
    public async Task Handle_Postprandial1h_ConDiabetes_ClassifiesByCatalogBand(int value, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, DiabetesType = DiabetesType.Type2 };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            GlucoseReadings = [new GlucoseReading
            {
                ReadingType = GlucoseReadingType.PostBreakfast,
                ValueMgDl = value,
                PostprandialWindow = PostprandialWindow.OneHour,
            }],
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Postprandial1h)
            .Status.Should().Be(expected);
    }

    // RF-004 · postprandial_2h · ConDiabetes: same bands as 1h (180/250) in the catalog.
    [Theory]
    [InlineData(150, GoalStatus.InRange)]
    [InlineData(200, GoalStatus.AtRisk)]
    [InlineData(260, GoalStatus.OutOfRange)]
    public async Task Handle_Postprandial2h_ConDiabetes_ClassifiesByCatalogBand(int value, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, DiabetesType = DiabetesType.Type2 };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            GlucoseReadings = [new GlucoseReading
            {
                ReadingType = GlucoseReadingType.PostBreakfast,
                ValueMgDl = value,
                PostprandialWindow = PostprandialWindow.TwoHour,
            }],
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Postprandial2h)
            .Status.Should().Be(expected);
    }

    // RF-005a · systolic_bp · SinDiabetes: InRange < 120, AtRisk 120–129, OutOfRange ≥ 130.
    [Theory]
    [InlineData(110, GoalStatus.InRange)]
    [InlineData(125, GoalStatus.AtRisk)]
    [InlineData(135, GoalStatus.OutOfRange)]
    public async Task Handle_SystolicBp_SinDiabetes_ClassifiesByCatalogBand(int value, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            SystolicPressure = value,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.SystolicBp)
            .Status.Should().Be(expected);
    }

    // RF-005b · diastolic_bp · SinDiabetes: InRange < 80, AtRisk 80–89, OutOfRange ≥ 90.
    [Theory]
    [InlineData(70, GoalStatus.InRange)]
    [InlineData(85, GoalStatus.AtRisk)]
    [InlineData(95, GoalStatus.OutOfRange)]
    public async Task Handle_DiastolicBp_SinDiabetes_ClassifiesByCatalogBand(int value, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            DiastolicPressure = value,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.DiastolicBp)
            .Status.Should().Be(expected);
    }

    // RF-006 · heart_rate · Universal: InRange 60–100, AtRisk 50–59 or 101–110, OutOfRange < 50 or > 110.
    [Theory]
    [InlineData(70, GoalStatus.InRange)]
    [InlineData(105, GoalStatus.AtRisk)]
    [InlineData(115, GoalStatus.OutOfRange)]
    public async Task Handle_HeartRate_Universal_ClassifiesByCatalogBand(int value, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            HeartRate = value,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HeartRate)
            .Status.Should().Be(expected);
    }

    // RF-007 · bmi · Universal: InRange 18.5–24.9, AtRisk 25.0–29.9, OutOfRange ≥ 30.
    // Weight is back-computed from BMI × height² (height fixed at 170 cm) so the
    // (weight → bmi) computation in BuildItem yields exactly the target BMI value.
    [Theory]
    [InlineData(63.58, 22.0, GoalStatus.InRange)]   // 63.58 / 1.7² = 22.0
    [InlineData(78.03, 27.0, GoalStatus.AtRisk)]    // 78.03 / 1.7² = 27.0
    [InlineData(92.48, 32.0, GoalStatus.OutOfRange)] // 92.48 / 1.7² = 32.0
    public async Task Handle_Bmi_Universal_ClassifiesByCatalogBand(decimal weightKg, decimal expectedBmi, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            WeightKg = weightKg,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        var item = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Bmi);
        item.ValueUsed.Should().Be(expectedBmi);
        item.Status.Should().Be(expected);
    }

    // RF-008 · ldl_primary · SinDiabetes: InRange < 130, AtRisk 130–159, OutOfRange ≥ 160.
    [Theory]
    [InlineData(100, GoalStatus.InRange)]
    [InlineData(145, GoalStatus.AtRisk)]
    [InlineData(170, GoalStatus.OutOfRange)]
    public async Task Handle_LdlPrimary_SinDiabetes_ClassifiesByCatalogBand(decimal ldl, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m, HasAscvd = false };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = ldl,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlPrimary)
            .Status.Should().Be(expected);
    }

    // RF-009 · ldl_secondary · SinDiabetes: InRange < 100, AtRisk 100–129, OutOfRange ≥ 130.
    // HasAscvd = true routes the LDL field to ldl_secondary (stricter thresholds).
    [Theory]
    [InlineData(80, GoalStatus.InRange)]
    [InlineData(115, GoalStatus.AtRisk)]
    [InlineData(140, GoalStatus.OutOfRange)]
    public async Task Handle_LdlSecondary_SinDiabetes_ClassifiesByCatalogBand(decimal ldl, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m, HasAscvd = true };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Ldl = ldl,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.LdlSecondary)
            .Status.Should().Be(expected);
    }

    // RF-010 · hdl · Universal Female: InRange ≥ 50, AtRisk 40–49, OutOfRange < 40.
    // HDL is a low-only spec (no AtRiskHigh / OutOfRangeHigh), so the InRange/AtRisk/OutOfRange
    // band order is reversed from the rest of the grid: a *low* value is worse.
    [Theory]
    [InlineData(60, GoalStatus.InRange)]
    [InlineData(45, GoalStatus.AtRisk)]
    [InlineData(35, GoalStatus.OutOfRange)]
    public async Task Handle_Hdl_Universal_ClassifiesByCatalogBand(decimal hdl, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m, Gender = Gender.Female };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hdl = hdl,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Hdl)
            .Status.Should().Be(expected);
    }

    // RF-011 · total_cholesterol · Universal: InRange < 200, AtRisk 200–239, OutOfRange ≥ 240.
    [Theory]
    [InlineData(180, GoalStatus.InRange)]
    [InlineData(220, GoalStatus.AtRisk)]
    [InlineData(250, GoalStatus.OutOfRange)]
    public async Task Handle_TotalCholesterol_Universal_ClassifiesByCatalogBand(decimal total, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            TotalCholesterol = total,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.TotalCholesterol)
            .Status.Should().Be(expected);
    }

    // RF-012 · triglycerides · Universal: InRange < 150, AtRisk 150–499, OutOfRange ≥ 500
    // (pancreatitis threshold per ADA Sec. 10).
    [Theory]
    [InlineData(130, GoalStatus.InRange)]
    [InlineData(300, GoalStatus.AtRisk)]
    [InlineData(550, GoalStatus.OutOfRange)]
    public async Task Handle_Triglycerides_Universal_ClassifiesByCatalogBand(decimal tg, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Triglycerides = tg,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Triglycerides)
            .Status.Should().Be(expected);
    }

    // RF-013 · creatinine · Universal Female: InRange 0.5–1.1, AtRisk 1.2–1.4, OutOfRange > 1.4.
    [Theory]
    [InlineData(1.0, GoalStatus.InRange)]
    [InlineData(1.3, GoalStatus.AtRisk)]
    [InlineData(1.5, GoalStatus.OutOfRange)]
    public async Task Handle_Creatinine_Universal_ClassifiesByCatalogBand(decimal creatinine, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m, Gender = Gender.Female };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Creatinine = creatinine,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Creatinine)
            .Status.Should().Be(expected);
    }

    // RF-014 · egfr · Universal: InRange ≥ 60, AtRisk 30–59, OutOfRange < 30.
    // eGFR is *derived* via IEgfrCalculator from creatinine + age + gender, not measured
    // directly. Override the default calculator stub (which returns a healthy 95m whenever
    // creatinine is present) so each band gets the right value, mirroring the existing
    // Handle_WhenCreatininePresent_EvaluatesEgfrFromCalculatorOutput test.
    [Theory]
    [InlineData(90, GoalStatus.InRange)]
    [InlineData(45, GoalStatus.AtRisk)]
    [InlineData(20, GoalStatus.OutOfRange)]
    public async Task Handle_Egfr_Universal_ClassifiesByCatalogBand(decimal egfrValue, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            HeightCm = 170m,
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1976, 1, 1),
        };

        // Creatinine is set so the handler feeds the calculator; the calculator stub is
        // overridden per case to return the band-specific eGFR.
        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Creatinine = 1.0m,
        };

        _egfrCalculator
            .Calculate(1.0m, Arg.Any<int>(), Gender.Male)
            .Returns(egfrValue);

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Egfr)
            .Status.Should().Be(expected);
    }

    // RF-015 · bun · Universal: InRange 7–20, AtRisk 21–40, OutOfRange < 7 or > 40.
    [Theory]
    [InlineData(15, GoalStatus.InRange)]
    [InlineData(30, GoalStatus.AtRisk)]
    [InlineData(45, GoalStatus.OutOfRange)]
    public async Task Handle_Bun_Universal_ClassifiesByCatalogBand(decimal bun, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Bun = bun,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns(labResult);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.Bun)
            .Status.Should().Be(expected);
    }

    // RF-016 · waist_circumference · Universal Female: InRange < 80, AtRisk 80–88, OutOfRange > 88.
    [Theory]
    [InlineData(75, GoalStatus.InRange)]
    [InlineData(85, GoalStatus.AtRisk)]
    [InlineData(90, GoalStatus.OutOfRange)]
    public async Task Handle_WaistCircumference_Universal_ClassifiesByCatalogBand(int value, GoalStatus expected)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m, Gender = Gender.Female };

        var dailyRecord = new DailyRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 6, 21),
            WaistCm = value,
        };

        SetupAuth(userId, patientId, patient);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([dailyRecord]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.WaistCircumference)
            .Status.Should().Be(expected);
    }

    // ====================================================================================
    // IsCustomGoal flag: surfaced on the GoalEvaluationItemResult so the FE can badge
    // ParametroMeta cards with "Ajustada por tu doctor" when the doctor has set a custom
    // ClinicalGoal for that parameter. The flag must follow the same routing as
    // ThresholdUsed: true when ApplyCustom actually merged a custom band (or SpecFromCustom
    // built the spec from a custom band in the pregnancy-no-catalog-row branch), false when
    // only the ADA catalog spec was used.
    // ====================================================================================

    [Fact]
    public async Task Handle_WhenClinicalGoalExistsForParameter_ItemHasIsCustomGoalTrue()
    {
        // Arrange — same shape as the existing custom-override test (line 367): non-pregnant
        // patient with an HbA1c custom goal. IsCustomGoal must be true for that item.
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 7.5m,
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
        hba1cItem.IsCustomGoal.Should().BeTrue("the doctor set a custom HbA1c goal, so the item must carry the flag");
    }

    [Fact]
    public async Task Handle_WhenNoClinicalGoalForParameter_ItemHasIsCustomGoalFalse()
    {
        // Arrange — same patient/lab as the InRange test, but no custom goals at all. The
        // HbA1c item must surface IsCustomGoal=false so the FE does not show the badge.
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId, HeightCm = 170m };

        var labResult = new LabResult
        {
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 6, 21),
            Hba1c = 5.0m,
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
        var hba1cItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.HbA1c);
        hba1cItem.IsCustomGoal.Should().BeFalse("no custom goal exists, the threshold came from the ADA catalog");
    }

    // Pregnancy + SBP custom + no reading → NoData item. The flag must still be true so the
    // FE can tell the patient "this parameter was set up for you by your doctor; bring a
    // reading to your next visit" rather than implying the metric is irrelevant to them.
    [Fact]
    public async Task Handle_WhenPregnantAndSbpCustomGoalButNoReading_NoDataItemHasIsCustomGoalTrue()
    {
        // Arrange — mirrors Handle_WhenPregnantAndSbpCustomGoalButNoReading_EmitsNoDataWithSpecialistReason
        // (line 787), but the new assertion is the IsCustomGoal flag on the NoData item.
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
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([customGoal]);

        // Act
        ErrorOr<EvaluateGoalsResult> result =
            await _handler.Handle(new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var sbpItem = result.Value.Items.First(i => i.ParameterId == AdaGoalConstants.SystolicBp);
        sbpItem.Status.Should().Be(GoalStatus.NoData);
        sbpItem.IsCustomGoal.Should().BeTrue("a doctor-set SBP goal exists for this pregnant patient even with no reading");
    }
}
